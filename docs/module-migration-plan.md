# 模块分批迁移计划（Java → .NET）

> 2026-08-16 制定。依据：Java 侧 89 个 WebController + 10 个 PDA + 1 个 Oss（comm）控制器的全量扫描，
> 以及 152 个实体间 `xxxId` 字段引用的静态依赖分析（被引用次数 = 有多少其他实体外键依赖它）。
> 执行方式：每批 1 个流水线任务（可拆成每模块 1 个 taskId），业务侧代码一律走 MCP 流水线，Claude 逐批验收。

## 依赖分析结论

被引用最多的主数据（必须最先就位，否则下游模块没法迁）：

```
34  Sku          25  Type        20  Site        20  Supplier
10  WarehouseLocation   9  Asin    8  User        6  Warehouse
 5  Container     5  ShippingOrder   5  WarehouseOut
 4  Tenant        4  Department
```

引用别人最多的复杂单据（最后迁）：
`ProcurementOrderDetails`(7)、`LogisticsShipmentBaseInfo`(7)、`PurchaseShipment`(6)、
`ShippingOrderContainer`(6)、`Sku`(6)、`FactoryOrderDetails`(5)、`WarehouseCheckMp`(5)。

## 迁移原则

1. **顺序**：系统权限地基 → 字典/主数据小表 → SKU 核心 → 仓储 → 采购 → 船务物流 → 财务 → 报表 → PDA。
2. **缓存红利**：字典小表和主数据（下拉高频、变更低频）迁移时标 `[RedisCacheable]`；单据大表不标。
3. **每批验收**：流水线 awaiting_review → Claude 静态审查 diff → approved/rework；构建 0 警告 0 错误。
4. 每模块四层结构照币别模板（`CurrencyManagement`），路由/HTTP 方法与 Java 逐一勾对。

## 已完成

| 模块 | 说明 |
| --- | --- |
| Login（web）/ User（部分） | 验证码 + accountLogin + refreshToken 已跑通；User 仅有 GetByAccountAsync |
| CurrencyManagement（币别） | 模板模块：四层 + Facet + UniqueConstraint + RedisCacheable + 删除存在性检查 |

## 批次清单

### 批次 1 — 系统权限地基（6 个）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| User（补全） | /user/web | 用户 CRUD + 角色绑定；被引用 8 次 |
| Role | /role/web | 角色；建议 RedisCacheable |
| Resource | /resource/web | 菜单/权限资源；建议 RedisCacheable |
| Authorization (+AuthorizedLog) | /authorization/web, /authorizedLog/web | 授权与授权日志 |
| Department | /department/web | 部门；被引用 4 次；建议 RedisCacheable |
| Tenant | /tenant/web | 租户；被引用 4 次 |

### 批次 2 — 数据字典与系统日志（7 个）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| DataDictionaryType | /dataDictionaryType/web | 字典类型；建议 RedisCacheable |
| DataDictionary | /dataDictionary/web | 字典项；建议 RedisCacheable |
| Message | /message/web | 站内消息 |
| Task | /task/web | 任务 |
| UserLoginLog | /userLoginLog/web | 只读日志，无写接口则只做查询 |
| UserOperationLog | /userOperationLog/web | 同上 |
| ServiceLog | /serviceLog/web | 同上 |

### 批次 3 — 通用主数据小表（7 个，全部建议 RedisCacheable）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| UnitManagement | /unitManagement/web | 计量单位 |
| Country | /country/web | 国家 |
| Area | /area/web | 地区 |
| Site | /site/web | 站点；**被引用 20 次** |
| Type | /type/web | 类型；**被引用 25 次** |
| Category | /category/web | 分类 |
| Port | /port/web | 港口 |

### 批次 4 — 产品属性/辅助主数据（8 个，全部建议 RedisCacheable）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| Model | /model/web | 型号 |
| Property | /property/web | 属性 |
| LabelGroup | /labelGroup/web | 标签组 |
| ColorBox (+ColorBoxArchive) | /ColorBox/web, /colorBoxArchive/web | 彩盒与彩盒档案 |
| Container | /container/web | 柜型；被引用 5 次 |
| DestinationWarehouse | /destinationWarehouse/web | 目的仓 |
| LogisticsMethod | /logisticsMethod/web | 物流方式 |
| Store | /store/web | 店铺 |

### 批次 5 — 供应商（3 个）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| Supplier | /supplier/web | **被引用 20 次**；建议 RedisCacheable |
| SupplierCollectionType | /supplierCollectionType/web | 供应商汇集类型 |
| SupplierInventory | /supplierInventory/web | 供应商库存（依赖 Site/Sku/Supplier，需批次 6 后完善） |

### 批次 6 — SKU 核心（9 个）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| Asin | /asin/web | 被引用 9 次 |
| Product | /product/web | 产品 |
| SkuBaseInfo | /skuBaseInfo/web | SKU 基础信息 |
| Sku | /sku/web | **被引用 34 次，全库核心**；依赖 Asin/Site/Supplier/SkuSupplier 等 6 个 |
| SkuSupplier | /skuSupplier/web | SKU-供应商 |
| SkuCartonSize | /skuCartonSize/web | 箱规 |
| SkuQuotedPrice | /skuQuotedPrice/web | 报价 |
| SkuIteration | /skuIteration/web | SKU 迭代 |
| Cabinet (+CabinetQc) | /cabinet/web, /CabinetQc/web | 柜与柜质检 |

### 批次 7 — 仓库主数据（4 个，建议 RedisCacheable）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| Warehouse | /warehouse/web | 被引用 6 次 |
| WarehouseArea | /warehouseArea/web | 库区 |
| WarehouseShelf | /warehouseShelf/web | 货架 |
| WarehouseLocation | /warehouseLocation/web | **库位，被引用 10 次**；依赖 Type/Warehouse/Area/Shelf |

### 批次 8 — 库存（2 个）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| Stock | /stock/web | 依赖 Sku/Type/WarehouseLocation |
| Specimen | /specimen/web | 样品（依赖 Asin/Sku/Supplier/Warehouse） |

### 批次 9 — 采购计划（7 个）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| ProcurementPlan | /procurementPlan/web | 采购计划（+Details） |
| ProcurementTask | /procurementTask/web | 采购任务 |
| MaterialDistribution | /materialDistribution/web | 物料分配 |
| SalesForecast | /salesForecast/web | 销售预测 |
| Replenishment | /replenishment/web | 补货 |
| DeliveryPlan | /deliveryPlan/web | 交付计划 |
| Marketing | /marketing/web | 营销 |

### 批次 10 — 采购订单（3 个，复杂单据）
| 模块 | 路由 | 备注 |
| --- | --- | --- |
| FactoryOrder | /factoryOrder/web | 工厂订单（+Details，被引用 3 次） |
| ProcurementOrder | /procurementOrder/web | **采购订单明细依赖 7 个实体，全库最复杂** |
| PurchaseShipment | /purchaseShipment/web | 采购发货（依赖 6 个实体） |

### 批次 11 — 入库（2 个）
WarehouseInNotice（/warehouseInNotice/web 入库通知）、WarehouseIn（/warehouseIn/web 入库）

### 批次 12 — 出库（1 个，含子表 WarehouseOutSku/WarehouseOutReceive 等）
WarehouseOut（/WarehouseOut/web，被引用 5 次）

### 批次 13 — 库内作业（3 个）
WarehouseMove（移库）、WarehouseChange（库存变更）、WarehouseCheck（盘点，WarehouseCheckMp 依赖 5 个）

### 批次 14 — 船务（4 个）
ShippingPlan（+Details）、ShippingOrder（+Container 子表，被引用 5 次）、InitialItinerary、InitialItineraryShipment

### 批次 15 — 物流（4 个）
Logistics、LogisticsSku、LogisticsShipment（BaseInfo 依赖 7 个实体）、LogisticsFinanceApprove

### 批次 16 — 财务（7 个）
FinanceAccount、FinanceAccountType、FinanceAccountTypeExpenseMp、FinanceInvoice、ExpenseManagement、ExchangeRateManagement（依赖币别）、ChargeDetails

### 批次 17 — 应收应付（2 个）
PayAble（/payAble/web）、Receivable（/receivable/web）

### 批次 18 — 审批与报表（2 个）
BillApprovalManagement（单据审批）、BusinessReport（经营报表）

### 批次 19 — PDA + 杂项（11 个）
PDA：Login、User、Stock、WarehouseIn、WarehouseInNotice、WarehouseOut、WarehouseMove、WarehouseChange、WarehouseCheck、WarehouseLocation（复用对应 Web 批次的服务层，只做端点）；comm：OssController（文件上传，需先定 OSS 方案）

## 流水线任务提示模板（每模块）

```
迁移模块 {Name}（对应 Java {Name}WebController，路由 /api{route}）：
1. 先读 Java 侧 controller（wuxi-api/service/.../web/{Name}WebController.java）、
   service impl（service/impl/{Name}ServiceImpl.java）、mapper XML，对齐路由/方法/出入参与业务语义；
2. 按 AGENTS.md 硬性规定与币别 CurrencyManagement 模板落四层（Domain/Repository/Service/Endpoint）；
3. 字典/主数据类实体标 [RedisCacheable(30, RedisExpireType.Day)]；防重字段标 [UniqueConstraint]；
4. 老库表审计列缺失标 [AuditIgnore]；请求 DTO 标 [Facet] 反向映射，响应 DTO partial class + Facet；
5. dotnet build 0 警告 0 错误。
```
