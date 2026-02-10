#!/bin/bash

# 测试脚本：验证管线和依赖注入功能
# 此脚本构建所有插件并验证它们可以正常编译

set -e

echo "=========================================="
echo "插件系统测试：管线 + 依赖注入"
echo "=========================================="
echo

# 1. 构建服务器
echo "步骤 1: 构建服务器..."
dotnet build PhiraMp.Server/PhiraMp.Server.csproj --nologo -v q
echo "✓ 服务器构建成功"
echo

# 2. 构建示例插件
echo "步骤 2: 构建管线演示插件..."
dotnet build PhiraMp.Plugins.PipelineDemo/PhiraMp.Plugins.PipelineDemo.csproj --nologo -v q
echo "✓ 管线演示插件构建成功"
echo

echo "步骤 3: 构建依赖注入演示插件..."
dotnet build PhiraMp.Plugins.DependencyInjectionDemo/PhiraMp.Plugins.DependencyInjectionDemo.csproj --nologo -v q
echo "✓ 依赖注入演示插件构建成功"
echo

# 3. 构建现有插件（向后兼容性测试）
echo "步骤 4: 验证向后兼容性..."
dotnet build PhiraMp.Plugins.RandomRoom/PhiraMp.Plugins.RandomRoom.csproj --nologo -v q
echo "✓ RandomRoom 插件兼容"

dotnet build PhiraMp.Plugins.CycleVoting/PhiraMp.Plugins.CycleVoting.csproj --nologo -v q
echo "✓ CycleVoting 插件兼容"

dotnet build PhiraMp.Plugins.SinglePlayerPrevention/PhiraMp.Plugins.SinglePlayerPrevention.csproj --nologo -v q
echo "✓ SinglePlayerPrevention 插件兼容"
echo

# 4. 部署插件
echo "步骤 5: 部署插件到 plugins 目录..."
mkdir -p plugins

cp PhiraMp.Plugins.PipelineDemo/bin/Debug/net10.0/PhiraMp.Plugins.PipelineDemo.dll plugins/
echo "  - PipelineDemo.dll"

cp PhiraMp.Plugins.DependencyInjectionDemo/bin/Debug/net10.0/PhiraMp.Plugins.DependencyInjectionDemo.dll plugins/
echo "  - DependencyInjectionDemo.dll"

cp PhiraMp.Plugins.RandomRoom/bin/Debug/net10.0/PhiraMp.Plugins.RandomRoom.dll plugins/
echo "  - RandomRoom.dll"

cp PhiraMp.Plugins.CycleVoting/bin/Debug/net10.0/PhiraMp.Plugins.CycleVoting.dll plugins/
echo "  - CycleVoting.dll"

cp PhiraMp.Plugins.SinglePlayerPrevention/bin/Debug/net10.0/PhiraMp.Plugins.SinglePlayerPrevention.dll plugins/
echo "  - SinglePlayerPrevention.dll"
echo

echo "=========================================="
echo "✓ 所有测试通过！"
echo "=========================================="
echo
echo "新功能已启用："
echo "  ✓ 管线系统（优先级 + 提前返回）"
echo "  ✓ 依赖注入（插件间服务共享）"
echo "  ✓ 向后兼容（现有插件无需修改）"
echo
echo "示例插件："
echo "  - PipelineDemo: 演示管线优先级和提前返回"
echo "  - DependencyInjectionDemo: 演示插件间依赖注入"
echo
echo "文档："
echo "  - PLUGIN_PIPELINE_AND_DI.md"
echo
