# 项目文件编码说明

## 为什么注释会乱码？

乱码（如 `涓撲笟绾?7 涓?Tab 鍐呭鍒嗗彂`）是因为 **编码不一致**：

1. **文件实际编码**：部分 `.cs` 可能曾被以 **GBK/GB2312**（中文 Windows 默认）保存。
2. **编辑器/工具假设**：Cursor、VS、或 Git 按 **UTF-8** 打开或保存，同一份字节就会变成乱码（UTF-8 字节被当成 Latin-1/GBK 解释，或反过来）。

## 正确做法

- **统一使用 UTF-8**：所有 C#、XML、MD 等文本文件请统一保存为 **UTF-8**（建议带 BOM，便于 Windows 下部分工具识别）。
- **Cursor / VSCode**：
  - 右下角点击当前编码（如 "UTF-8" 或 "GBK"），选 **"Reopen with Encoding"** → 若已是乱码，可试 **"UTF-8"** 或 **"GB2312"** 看哪种能正确显示中文。
  - 再选 **"Save with Encoding"** → 选 **"UTF-8"**，保存后该文件即统一为 UTF-8。
- **本仓库**：新改动的注释会以 UTF-8 写入；若你本地仍用 GBK 打开，会只在新加/修改的行显示正常，旧行仍乱码，需整文件用 "Save with Encoding" 存成 UTF-8。

## 批量转存为 UTF-8（可选）

在项目根目录用 PowerShell 将某目录下所有 `.cs` 转为 UTF-8（示例，谨慎执行前请备份）：

```powershell
Get-ChildItem -Path Source -Recurse -Filter *.cs | ForEach-Object {
  $content = Get-Content $_.FullName -Raw -Encoding Default
  [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($true))
}
```

若当前文件已是 UTF-8 误读为 GBK，则应用 "Reopen with Encoding" → UTF-8，再 "Save with Encoding" → UTF-8。
