@echo off
REM 设置编码为UTF-8
chcp 65001

REM 定义路径变量
set sourceFolder=data
set targetFolder=bin\Release\net8.0-windows\publish\win-x64\data

REM 复制到publish文件夹
IF EXIST "%targetFolder%\" (
    rmdir /S /Q "%targetFolder%"
    if %errorlevel% neq 0 (
        echo 删除目标文件夹失败
        exit /b 1
    )
)

IF NOT EXIST "%targetFolder%\" (
    mkdir "%targetFolder%"
    if %errorlevel% neq 0 (
        echo 创建目标文件夹失败
        exit /b 1
    )
)

xcopy "%sourceFolder%" "%targetFolder%" /E /Y
if %errorlevel% neq 0 (
    echo 复制文件失败
    exit /b 1
)

echo 文件复制成功
exit /b 0
