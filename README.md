# 小秘书记账机器人 (XiaoMiShu Bot)

一个基于 .NET 8 和 Telegram Bot API 开发的智能记账机器人，支持汇率管理、订单统计、管理员权限控制等功能。

## 📋 项目简介

小秘书机器人是一个功能丰富的 Telegram 机器人，主要用于：
- 💰 智能记账和订单管理
- 🌍 实时汇率查询和设置
- 👥 多级管理员权限控制
- 📊 数据统计和报表生成
- 🔧 基于 Blazor 的管理后台

记账机器人演示: @Numct_Bot
联系开发者 Telegram: @yoyoyo241026

## ✨ 主要功能

### 🤖 机器人功能
- **基本命令**: 开始、帮助、ID查询、时间显示
- **汇率管理**: 实时汇率查询、自定义汇率设置
- **记账操作**: 入款/下发记录、多币种支持、自动计算
- **权限管理**: 多级管理员权限、操作员管理
- **数据统计**: 订单统计、财务报表、历史记录

### 🖥️ 管理后台
- **用户管理**: 成员信息、权限设置、封禁管理
- **订单管理**: 订单查看、统计分析、数据导出
- **系统设置**: 汇率配置、费率设置、系统参数
- **消息管理**: 消息记录、违规检测、内容审核

## 🛠️ 技术栈

- **后端框架**: .NET 8 Web API
- **前端框架**: Blazor Server + AdminBlazor
- **数据库**: SQLite (支持 MySQL)
- **ORM**: FreeSql
- **日志系统**: Serilog
- **容器化**: Docker + Docker Compose
- **Bot框架**: TelegramBotBase

## 📦 项目结构

```
XiaoMiShu_Bot/
├── Components/                 # Blazor 组件
│   ├── Blog/                  # 博客相关组件
│   ├── JZ/                    # 记账相关组件
│   ├── Pages/                 # 页面组件
│   └── Setting/               # 设置相关组件
├── Configs/                   # 配置文件
├── Entities/                  # 数据实体
│   ├── Blog/                  # 博客实体
│   ├── JZ/                    # 记账实体
│   └── Setting/               # 设置实体
├── TelegramBot/               # Telegram 机器人核心
│   ├── Forms/                 # 表单处理
│   │   ├── Commands/          # 命令处理
│   │   └── MessageHandler.cs  # 消息处理器
│   ├── Services/              # 业务服务
│   └── Utils/                 # 工具类
├── wwwroot/                   # 静态资源
├── Logs/                      # 日志文件
├── keys/                      # 数据保护密钥
└── README_BOT_USAGE.md        # 机器人使用说明
```

## 🚀 快速开始

### 环境要求

- .NET 8.0 SDK
- Docker (可选)
- SQLite 数据库

### 1. 克隆项目

```bash
sudo apt-get update
sudo apt-get install git

git clone https://github.com/3bDjrvHs50kiZIJb5/XiaoMiShu_Bot.git
cd XiaoMiShu_Bot
```

### 2. 配置设置

复制配置文件模板,并修改配置：

```bash
cp appsettings_Sample.json appsettings.json
```

然后编辑 `appsettings.json` 文件：

```json
{
  "AllowedOrigins": [
    "http://服务器IP:5173"
  ],
  "Domain": "http://服务器IP:8003",
  "TelegramBot": {
    "ApiKey": "your-telegram-bot-api-key",
    "BotName": "your_bot_name",
    "BotCnName": "小秘书计算器"
  },
  "DetailedErrors": true
}
```

### 3. 运行项目

#### Docker 运行

下载Docker镜像(推荐Ubuntu系统)

```bash

sudo su -

# 下载并执行Docker官方安装脚本
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo systemctl start docker
sudo systemctl enable docker

# 运行自动脚本,创建数据库和启动容器
sudo ./docker-auto.sh

# 以后升级就自动拉取 执行 
sudo ./docker-pull.sh

# 查看log日志
sudo docker logs -f jzbot-back

```

# 访问管理后台
http://服务器IP:8003

> 管理员账号: admin 密码: admin

```
# 添加菜单
JZ/JzChat	
JZ/JzOrder	
Setting/Banned	
Setting/Member	
Setting/Messages
```


### 4. 访问应用

- **Web 管理后台**: http://服务器IP:8003

## 📱 机器人使用说明

### 基本命令

| 命令 | 描述 | 权限 |
|------|------|------|
| `/start` | 启动机器人 | 所有用户 |
| `/help` | 显示帮助信息 | 所有用户 |
| `/id` | 显示用户信息 | 所有用户 |
| `/时间` | 显示当前时间 | 所有用户 |

### 管理员命令

| 命令 | 描述 | 权限 |
|------|------|------|
| `/添加操作员 @用户名` | 添加操作人员 | 管理员 |
| `/移除操作员 @用户名` | 移除操作人员 | 管理员 |
| `/操作员列表` | 查看操作人员列表 | 管理员 |

### 汇率管理

| 命令 | 描述 | 权限 |
|------|------|------|
| `/汇率` | 查看群组汇率 | 所有用户 |
| `/Z0` | 查看 USDT 汇率 | 所有用户 |
| `/设置汇率 数值` | 设置群组汇率 | 管理员 |

### 记账操作

| 命令 | 描述 | 示例 |
|------|------|------|
| `+金额` | 记录入款 | `+100` |
| `-金额` | 记录下发 | `-50` |
| `+金额u` | 记录入款(USD) | `+100u` |
| `-金额u` | 记录下发(USD) | `-50u` |
| `+0` | 查看入款统计 | - |
| `-0` | 查看下发统计 | - |
| `清除今日账单` | 清除今日账单 | - |
| `45*5` | 计算器功能 | - |

## 🔧 配置说明

### 环境变量

| 变量名 | 描述 | 默认值 |
|--------|------|--------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | Production |
| `ASPNETCORE_URLS` | 监听地址 | http://+:80 |

### 数据库配置

项目默认使用 SQLite 数据库，数据库文件为 `Sqlite.db`。如需使用 MySQL，请修改 `Program.cs` 中的连接字符串。

### 日志配置

日志文件保存在 `Logs/` 目录下，按天滚动。支持控制台和文件两种输出方式。


## 📊 数据管理

### 数据库初始化

项目启动时会自动创建数据库表结构，无需手动初始化。

### 数据备份

建议定期备份以下文件：
- `Sqlite.db` - 数据库文件
- `Logs/` - 日志文件
- `keys/` - 数据保护密钥

### 数据迁移

如需从 SQLite 迁移到 MySQL：

1. 修改 `Program.cs` 中的连接字符串
2. 安装 MySQL 提供程序包
3. 重新启动应用

## 🔒 安全说明

- 请妥善保管 Telegram Bot API Key
- 定期更新依赖包以修复安全漏洞
- 生产环境请使用 HTTPS
- 建议定期备份重要数据

## 🐛 故障排除

### 常见问题

1. **机器人无响应**
   - 检查 API Key 是否正确
   - 确认网络连接正常
   - 查看日志文件排查错误

2. **数据库连接失败**
   - 检查数据库文件权限
   - 确认连接字符串正确
   - 查看应用日志

3. **管理后台无法访问**
   - 检查端口是否被占用
   - 确认防火墙设置
   - 查看 CORS 配置

### 日志查看

```bash
# 查看实时日志
Logs/log-$(date +%Y%m%d).txt

```

## 🤝 贡献指南

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 📞 支持

如有问题或建议，请通过以下方式联系：

- 创建 Issue
- 发送邮件
- 提交 Pull Request

## 🔄 更新日志

### v1.0.0
- 初始版本发布
- 基础记账功能
- 汇率管理
- 管理员权限控制
- Web 管理后台

---

