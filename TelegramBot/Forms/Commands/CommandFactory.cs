namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 命令工厂类 - 负责创建和管理所有命令实例
    /// </summary>
    public static class CommandFactory
    {
        // 命令实例缓存字典 - 避免重复创建实例
        private static readonly Dictionary<string, ICommand> _commandInstances = new Dictionary<string, ICommand>();

        /// <summary>
        /// 静态构造函数 - 初始化所有命令实例
        /// </summary>
        static CommandFactory()
        {
            // 管理员相关命令
            _commandInstances["HandleAddAdminCommandAsync"] = new AddAdminCommand();
            _commandInstances["HandleRemoveAdminCommandAsync"] = new RemoveAdminCommand();
            _commandInstances["HandleAdminListCommandAsync"] = new AdminListCommand();

            // 课程管理命令
            // _commandInstances["HandleStartClassCommandAsync"] = new StartClassCommand();
            // _commandInstances["HandleEndClassCommandAsync"] = new EndClassCommand();

            // 订单相关命令
            _commandInstances["HandleOrderAddCommandAsync"] = new OrderAddCommand();
            _commandInstances["HandleOrderDivideCommandAsync"] = new OrderDivideCommand();
            _commandInstances["HandleClearTodayOrderCommandAsync"] = new ClearTodayOrderCommand();

            // 汇率相关命令
            _commandInstances["HandleExchangeRateCommandAsync"] = new ExchangeRateCommand();
            _commandInstances["HandleOTCExchangeRateCommandAsync"] = new OTCExchangeRateCommand();
            _commandInstances["HandleSetExchangeRateCommandAsync"] = new SetExchangeRateCommand();

            // 费率相关命令
            _commandInstances["HandleFeeRateCommandAsync"] = new FeeRateCommand();
            _commandInstances["HandleSetFeeRateCommandAsync"] = new SetFeeRateCommand();

            // 基础功能命令
            _commandInstances["HandleStartCommandAsync"] = new StartCommand();
            _commandInstances["HandleHelpCommandAsync"] = new HelpCommand();
            _commandInstances["HandleIdCommandAsync"] = new IdCommand();
            _commandInstances["HandleTimeCommandAsync"] = new TimeCommand();
        }

        /// <summary>
        /// 根据处理器名称获取对应的命令实例
        /// </summary>
        /// <param name="handlerName">处理器名称</param>
        /// <returns>命令实例，如果找不到则返回null</returns>
        public static ICommand GetCommand(string handlerName)
        {
            _commandInstances.TryGetValue(handlerName, out var command);
            return command;
        }

        /// <summary>
        /// 检查是否存在指定的命令处理器
        /// </summary>
        /// <param name="handlerName">处理器名称</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        public static bool HasCommand(string handlerName)
        {
            return _commandInstances.ContainsKey(handlerName);
        }

        /// <summary>
        /// 获取所有可用的命令处理器名称
        /// </summary>
        /// <returns>命令处理器名称列表</returns>
        public static IEnumerable<string> GetAllCommandNames()
        {
            return _commandInstances.Keys;
        }
    }
}