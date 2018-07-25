# ExportLuaUI
Unity热更新tolua框架NGUI对应lua脚本导出工具

## 使用方法
1. 将ExportLuaUI.cs放入工程目录Editor文件夹内
2. 右键点击目标Prefab(支持多选),选择"Export Lua UI Script"<br><br>![](https://github.com/glegoo/ExportLuaUI/blob/master/Res/a.png?raw=true)

## 注意事项
1. static节点下的UI脚本不会被导出.
2. 所有lab开头(大小写敏感, 下同)的UILabel, spr开头的UISprite, btn开头包含碰撞的节点为自动生成代码对象.
3. 节点按照驼峰命名,否则无法达到最佳效果. 如: labText, sprIcon, btnCloseWindow.
4. 导出时存在同名文件, 如果选择保存两者, 则在同目录中生成filename_auto.lua.<br><br>![](https://github.com/glegoo/ExportLuaUI/blob/master/Res/b.png?raw=true)

## 实际效果
![](https://github.com/glegoo/ExportLuaUI/blob/master/Res/c.png?raw=true)
