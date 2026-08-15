-- Wuxi.Shengshen.Erp 用户表初始化（基座阶段最小化字段）
-- 对应表：user（与 Java 端同表复用，注意列名小写下划线）
-- 密码：admin123 经 PasswordUtil.Encode(<id>, "admin123") 计算（BCrypt $2a$）。
-- 首次启动建议：先插入 root 账号，拿到自增 id 后用 C# 工具/在线 BCrypt 工具按 id+明文 重算哈希再 update。

CREATE TABLE IF NOT EXISTS `user` (
    `id           ` BIGINT NOT NULL COMMENT '主键（雪花ID）',
    `name         ` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '姓名',
    `account      ` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '账号',
    `password     ` VARCHAR(255) NOT NULL DEFAULT '' COMMENT '密码（BCrypt）',
    `department_id` BIGINT NULL COMMENT '所属部门ID',
    `email        ` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '邮箱',
    `tenant_id    ` BIGINT NULL COMMENT '租户ID',
    `is_disable   ` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否禁用',
    `is_delete    ` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否删除',
    `creator      ` BIGINT NULL,
    `create_by    ` VARCHAR(64) NULL,
    `create_time  ` DATETIME NULL,
    `updater      ` BIGINT NULL,
    `update_by    ` VARCHAR(64) NULL,
    `update_time  ` DATETIME NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_user_account` (`account`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户';

-- 测试账号示例（id=1，密码 admin123 的 BCrypt 占位哈希，使用前请按真实 id 重算）：
-- INSERT INTO user(id,name,account,password,is_disable,is_delete,create_time)
-- VALUES (1,'管理员','admin','$2a$10$REPLACE_WITH_REAL_BCRYPT_HASH_FOR_1admin123',0,0,NOW());