# Главное меню

Откройте Assets/Scenes/MainMenu.unity и нажмите Play.

- Играть: загружает сцену Laboratory.
- Настройки: громкость и полный экран; значения сохраняются автоматически.
- Назад или Escape: закрывает настройки.
- Выход: закрывает приложение; в редакторе останавливает Play Mode.

Вся логика находится в MainMenuController.cs. Разметка хранится в сцене,
а ссылки на панели и элементы управления видны на компоненте MenuCanvas.
Название игровой сцены можно поменять в поле Game Scene.

Canvas: Scale With Screen Size, 1280 x 720, Expand.
Для спрайтов отключены сглаживание и сжатие. Использованы Buttons.png
из Free-Basic-Pixel-Art-UI-for-RPG и TextBoxBigDark.png из 2DPixelUIPack.
Лицензии наборов находятся рядом. Шрифт Consolas используется для кириллицы.

В списке сборки MainMenu стоит первой, Laboratory второй.
Существующие сцены SampleScene и Laborotory сохранены без изменений.
