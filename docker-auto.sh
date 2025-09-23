

# 先构建新的镜像（不影响当前运行的容器）
docker compose build && 

# 构建成功后再停止当前运行的容器
docker compose down && 
# 最后启动新的容器
docker compose up -d