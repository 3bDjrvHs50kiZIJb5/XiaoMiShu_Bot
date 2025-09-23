# 先创建空文件Sqlite.db
# 如果 Sqlite.db 文件不存在，则创建一个空的 Sqlite.db 文件
if [ ! -f Sqlite.db ]; then
    touch Sqlite.db
    echo "已创建空的 Sqlite.db 文件。"
fi

# 先构建新的镜像（不影响当前运行的容器）
docker compose build && 

# 构建成功后再停止当前运行的容器
docker compose down && 
# 最后启动新的容器
docker compose up -d