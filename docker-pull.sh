#!/usr/bin/env bash

# 说明：极简一键部署脚本（无参数版）
# - 固定仓库：官方 XiaoMiShu_Bot 仓库
# - 固定分支：main
# - 目标目录：当前工作目录下的 XiaoMiShu_Bot
# - 步骤：校验依赖 -> 克隆或更新 -> 运行 ./docker-auto.sh
#
# 使用：
#   bash docker-auto-deploy.sh

set -euo pipefail

REPO_URL="https://github.com/3bDjrvHs50kiZIJb5/XiaoMiShu_Bot.git"
BRANCH="main"
TARGET_DIR=""

echo "========================================"
echo "[信息] 仓库地址 : $REPO_URL"
echo "[信息] 使用分支 : $BRANCH"
echo "[信息] 目标目录 : (待检测)"
echo "========================================"

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "[错误] 未检测到命令：$cmd，请先安装后重试。" >&2
    exit 1
  fi
}

check_dependencies() {
  # 基础依赖：git 必须；docker 与 compose 在运行前二次校验
  require_cmd git
}

detect_target_dir() {
  # 如果当前目录就是仓库（存在 .git 且存在 docker-auto.sh，或远程地址匹配），直接在当前目录执行更新
  if [[ -d .git ]]; then
    local is_project=false
    if [[ -f ./docker-auto.sh ]]; then
      is_project=true
    else
      # 进一步尝试匹配远程地址
      if git remote -v 2>/dev/null | grep -q "${REPO_URL}"; then
        is_project=true
      fi
    fi

    if [[ "$is_project" == true ]]; then
      TARGET_DIR="$(pwd)"
      echo "[信息] 检测到当前目录为项目仓库，将在当前目录执行更新。"
      echo "[信息] 目标目录 : $TARGET_DIR"
      return 0
    fi
  fi

  TARGET_DIR="$(pwd)/XiaoMiShu_Bot"
  echo "[信息] 未检测到当前目录为项目仓库，将使用目录：$TARGET_DIR"
}

clone_or_update_repo() {
  if [[ -d "$TARGET_DIR/.git" ]]; then
    echo "[步骤] 已存在仓库，执行增量更新..."
    pushd "$TARGET_DIR" >/dev/null
    git fetch --all --prune
    # 保持或切换到 main 分支
    current_branch="$(git rev-parse --abbrev-ref HEAD)"
    if [[ "$current_branch" != "$BRANCH" ]]; then
      git checkout "$BRANCH"
    fi
    git pull --ff-only origin "$BRANCH"
    popd >/dev/null
  else
    echo "[步骤] 目标目录不存在，执行克隆..."
    git clone --branch "$BRANCH" "$REPO_URL" "$TARGET_DIR"
  fi
}

run_project_deploy() {
  echo "[步骤] 执行项目部署脚本 ./docker-auto.sh ..."
  pushd "$TARGET_DIR" >/dev/null

  if [[ ! -f ./docker-auto.sh ]]; then
    echo "[错误] 未找到 ./docker-auto.sh，请确认仓库完整。" >&2
    popd >/dev/null
    exit 2
  fi
  chmod +x ./docker-auto.sh

  # 运行前做一次 docker / compose 简要校验（不自动安装，保持极简）
  if ! command -v docker >/dev/null 2>&1; then
    echo "[错误] 未检测到 docker，请先安装后重试。" >&2
    popd >/dev/null
    exit 3
  fi
  if ! docker compose version >/dev/null 2>&1 && ! command -v docker-compose >/dev/null 2>&1; then
    echo "[错误] 未检测到 docker compose（docker compose 或 docker-compose）。" >&2
    popd >/dev/null
    exit 4
  fi

  ./docker-auto.sh
  popd >/dev/null
}

main() {
  check_dependencies
  detect_target_dir
  clone_or_update_repo
  run_project_deploy
  echo "[完成] 部署流程已结束。"
}

main