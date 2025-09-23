# git clone -b docker https://<REMOVED>@github.com/zq535228/JZBot.git
# docker build -t jzbot-back .
# docker run -d -p 8003:80 --name jzbot-back jzbot-back
# docker-compose down && docker-compose up -d
# docker build -t jzbot-back . && docker-compose down && docker-compose up -d

# 第一阶段：构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["back.csproj", "./"]



# 还原包
RUN dotnet nuget list source
RUN dotnet restore --verbosity detailed

# 复制所有源代码
COPY . .
# 构建项目
RUN dotnet build "back.csproj" -c Release -o /app/build

# 发布项目
FROM build AS publish
RUN dotnet publish "back.csproj" -c Release -o /app/publish

# 第二阶段：运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 从发布阶段复制文件
COPY --from=publish /app/publish .

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# 暴露端口
EXPOSE 80

# 在 final 阶段添加
RUN mkdir -p /app/keys

# 启动应用
ENTRYPOINT ["dotnet", "back.dll"] 