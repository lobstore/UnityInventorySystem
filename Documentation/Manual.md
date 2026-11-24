# LOGIYGames Inventory — полное руководство

## 1. Назначение

LOGIYGames Inventory — пакет для хранения предметов в упорядоченных слотах, объединения одинаковых предметов в стаки, добавления и удаления количества, перемещения, разделения и передачи между инвентарями.

Runtime namespace: **LOGIYGames.Inventory**. Assembly: **LOGIYGames.Inventory**. Package: **com.logiygames.inventory**, версия **0.1.0**, целевая версия Unity из package manifest — **6000.6**.

В пакете есть два слоя:

- **Core Runtime**: ItemDefinition, ItemStack, ItemSlot, InventoryController. Их можно использовать без sample UI.
- **Optional Sample UI**: InventoryUIFactory, presenter/view-компоненты, контекстное меню, drag ghost, уведомление о получении предмета и адаптер PlayerInventoryUIBootstrap.

Пакет не реализует сохранение, валюту, экипировку, создание предмета в мире, сетевую синхронизацию и игровые правила использования предмета. Для интеграции предусмотрены события и ссылки на prefab.

Для автоматизированных инструментов и ИИ-агентов есть отдельный контракт **API.json** с точными сигнатурами, типами, return semantics и правилами изменения состояния.

## 2. Установка

1. Добавьте пакет в Unity-проект.
2. Убедитесь, что версия Unity соответствует 6000.6 или совместима с указанным package manifest.
3. Используйте namespace LOGIYGames.Inventory в runtime-скриптах.
4. Создайте ItemDefinition assets через Assets → Create → Inventory → Item → Data.
5. Добавьте InventoryController на GameObject персонажа, сундука или другого владельца инвентаря.
6. Настройте список Slots в Inspector, задав нужное количество ItemSlot элементов.

Runtime assembly definition не объявляет внешних assembly references. Для optional UI sample потребуются Unity UI, TextMeshPro и проектные интеграции, используемые PlayerInventoryUIBootstrap (PlayerManager, Character и GameManager). Эти типы являются зависимостями проекта, а не частью core inventory package.

## 3. Модель данных

### ItemDefinition

ItemDefinition — ScriptableObject, описывающий тип предмета, общие свойства и presentation assets.

| Поле | Назначение |
|---|---|
| Id | Стабильный уникальный строковый идентификатор для сейвов и внешних данных |
| DisplayName | Имя для интерфейса |
| Description | Описание предмета |
| Icon | Иконка |
| Prefab | Необязательный prefab предмета в мире |
| MaxStackSize | Максимальное количество единиц в одном слоте, минимум 1 |
| Sellable | Разрешает ли игра продавать предмет |
| SellPrice | Цена одной единицы, минимум 0 |
| Droppable | Разрешает ли игра выбрасывать предмет |

Создание asset: **Create → Inventory → Item → Data**.

Пример настройки:
- Potion: MaxStackSize = 10, Sellable = true, SellPrice = 5.
- Sword: MaxStackSize = 1, Sellable = true, SellPrice = 100.
- QuestKey: MaxStackSize = 1, Droppable = false.

MaxStackSize и SellPrice нормализуются через OnValidate в Editor. При изменении значений из кода также учитывайте эти ограничения.

**Важно об идентичности:** во время выполнения система сравнивает ItemDefinition по ссылке на ScriptableObject asset. Два разных assets с одинаковым Id не считаются одним предметом и не будут объединяться в стак. Id предназначен для сохранения/восстановления и интеграции с внешними данными; он не генерируется автоматически. Назначайте Id уникально и не меняйте его между версиями игры.

### ItemStack

ItemStack — сериализуемая структура, состоящая из:
- Data — ссылка на ItemDefinition;
- Amount — количество единиц.

Пустой стак — это стак без Data или с Amount меньше либо равным нулю. Конструктор ItemStack(data, amount) ограничивает отрицательное количество нулём. MaxStackSize берётся из ItemDefinition и нормализуется до значения не ниже единицы.

~~~csharp
using LOGIYGames.Inventory;

var potionStack = new ItemStack(potionDefinition, 7);
var onePotion = potionStack.WithAmount(1);
var copy = potionStack.Clone();

bool compatible = potionStack.CanStackWith(onePotion);
~~~

CanStackWith возвращает true только для двух непустых стеков с одной и той же ссылкой Data. Для сравнения не используются DisplayName или Id.

### ItemSlot

ItemSlot — сериализуемый класс, хранящий один ItemStack. Пустой слот хранит default ItemStack.

Основные члены:
- Item — текущее значение ItemStack (структура копируется при чтении).
- IsEmpty — проверка пустоты.
- OnItemChanged — UnityEvent, вызываемый при изменении Data или Amount.
- SetItem — заменяет содержимое.
- TryAdd — добавляет доступное количество и возвращает остаток.
- Clear — очищает слот.
- SwapWith — меняет содержимое двух слотов.

SetItem очищает слот при пустом значении и ограничивает количество MaxStackSize. Поэтому один слот не может содержать больше единиц, чем разрешено для соответствующего ItemDefinition. TryAdd возвращает количество, которое не поместилось; входной ItemStack не изменяется.

## 4. InventoryController

InventoryController — MonoBehaviour с упорядоченным сериализованным списком слотов. Добавьте его на GameObject владельца инвентаря.

Поле slots приватное и сериализуемое. Количество слотов задаётся в Inspector. В текущем API нет отдельного метода увеличения/уменьшения размера инвентаря во время игры. Если проекту нужна динамическая вместимость, реализуйте отдельный слой управления слотами.

При инициализации отсутствующие/null-элементы в сериализованном списке заменяются пустыми ItemSlot. Это предотвращает ошибки при обращении к незаполненным записям.

### Запросы

| Метод / свойство | Результат |
|---|---|
| Slots | Read-only список слотов в исходном порядке |
| IsFull | True, если пустых слотов нет |
| IsValidIndex(index) | Проверяет индекс |
| GetSlot(index) | Возвращает слот или null для неверного индекса |
| FindSlot(data) | Первый непустой слот с данным asset reference; иначе -1 |
| FindEmptySlot() | Первый пустой слот; иначе -1 |
| CountItem(data) | Суммарное количество во всех слотах |
| ContainsItem(data, amount = 1) | Есть ли минимум amount единиц |
| CanFit(stack) | Поместится ли полный стак с учётом частичных совместимых стеков |

CanFit не меняет содержимое инвентаря. Пустой входной стак считается помещающимся. ContainsItem требует непустой ItemDefinition; отрицательное amount возвращает false. Для корректной проверки наличия используйте положительное количество.

### Добавление предметов

AddItem принимает ItemStack или ItemDefinition с количеством и возвращает остаток, который не поместился. **Это не bool и не количество добавленных единиц.**

Алгоритм:
1. Инвентарь предварительно схлопывает совместимые частичные стаки через AutoStack.
2. Сначала заполняются существующие частичные стаки того же ItemDefinition.
3. Остаток распределяется по пустым слотам.
4. Вызывается OnItemAdded с фактически добавленным количеством, если добавлено больше нуля.
5. Возвращается неразмещённый остаток. Ноль означает, что весь стак принят.

~~~csharp
var remainder = inventory.AddItem(new ItemStack(potionDefinition, 27));

if (remainder > 0)
{
    Debug.LogWarning($"Inventory is full. Could not add {remainder} potions.");
}
~~~

Если у зелья MaxStackSize = 10 и в инвентаре достаточно места, 27 единиц распределятся как 10 + 10 + 7. Если места мало, метод вернёт фактический остаток.

### Удаление предметов

| Метод | Правила |
|---|---|
| RemoveItem(data, amount) | Удаляет из совпадающих слотов с конца списка. All-or-nothing: при нехватке ничего не меняет |
| RemoveFromSlot(index, amount) | Удаляет из конкретного слота; amount ограничивается количеством в слоте |
| RemoveAll(data) | Удаляет все стаки заданного предмета и возвращает общее количество |
| ClearSlot(index) | Очищает один слот |
| Clear() | Очищает весь инвентарь |

~~~csharp
bool removedAllOrNothing = inventory.RemoveItem(keyDefinition, 2);
bool removedFromSlot = inventory.RemoveFromSlot(3, 1);
int removedQuantity = inventory.RemoveAll(materialDefinition);
~~~

RemoveItem возвращает false, если data null, amount не положительный или предметов недостаточно. При успешном удалении OnItemRemoved получает ровно запрошенное количество.

RemoveFromSlot возвращает false для неверного индекса, пустого слота или неположительного amount. Если запрос больше количества в слоте, удаляется только то, что есть.

RemoveAll возвращает 0 для null или отсутствующего предмета. При удалении OnItemRemoved вызывается один раз с суммарным количеством. ClearSlot вызывает событие для прежнего стека; Clear вызывает событие отдельно для каждого непустого слота.

### Перемещение, объединение и обмен

TryMove(fromIndex, toIndex):
- если источник пуст или индексы неверны/совпадают — false;
- если цель пуста — весь стак переносится;
- если в цели тот же ItemDefinition — стаки объединяются до MaxStackSize; остаток остаётся в источнике;
- если предметы разные — содержимое слотов меняется местами.

MoveToFirstAvailable пытается объединить исходный стак с первым совместимым неполным стаком. Если такого нет, использует первый пустой слот. При частичном объединении остаток остаётся в исходном слоте.

SwapSlots(a, b) меняет содержимое двух корректных разных слотов, включая пустые.

~~~csharp
inventory.TryMove(0, 4);
inventory.MoveToFirstAvailable(2);
inventory.SwapSlots(1, 5);
~~~

### Разделение стака

SplitStack(fromIndex, amount) создаёт новый стак в первом пустом слоте.

SplitStack(fromIndex, toIndex, amount) разделяет в указанный пустой слот.

Условия:
- source и target должны быть разными корректными слотами;
- source не пустой;
- target пустой;
- amount больше нуля;
- amount меньше исходного количества, чтобы источник остался непустым.

~~~csharp
bool split = inventory.SplitStack(sourceIndex, 3);
~~~

Метод возвращает false, если любое из условий не выполнено.

### Передача в другой инвентарь

TransferTo(target, fromIndex, amount) передаёт количество из слота в другой InventoryController. Операция **частичная**: целевой инвентарь принимает столько, сколько может; источник уменьшает количество только на принятое значение.

~~~csharp
bool moved = playerInventory.TransferTo(chestInventory, sourceIndex, 12);
~~~

Метод возвращает false, если target null, target совпадает с source, источник/количество некорректны или целевой инвентарь не принял ни одной единицы. При успешной операции OnItemAdded срабатывает на target, а OnItemRemoved — на source.

### AutoStack

AutoStack заполняет ранние частичные стаки за счёт более поздних слотов с тем же ItemDefinition. Полностью поглощённые исходные слоты очищаются. Для каждого изменившегося слота вызывается OnItemChanged.

AutoStack не генерирует OnItemAdded и OnItemRemoved: содержимое инвентаря не увеличивается и не уменьшается, а только переорганизуется.

## 5. События и интеграция с игровыми системами

InventoryController публикует следующие UnityEvent<ItemStack>:
- OnItemAdded — реальное количество, добавленное через AddItem;
- OnItemRemoved — реальное количество, удалённое через методы удаления/очистки/передачи;
- OnItemUsed — событие использования;
- OnItemEquipped — событие экипировки;
- OnItemSold — событие продажи;
- OnItemDropped — событие выбрасывания;
- OnItemDiscarded — событие уничтожения.

OnItemUsed, OnItemEquipped, OnItemSold, OnItemDropped и OnItemDiscarded не вызываются core-операциями автоматически. Игра должна определить правила и момент этих действий. В sample UI обработчики продажи/выбрасывания/уничтожения сначала удаляют предмет и затем вызывают соответствующее событие. Валюту или world prefab нужно обрабатывать подписчиком.

~~~csharp
private void OnEnable()
{
    inventory.OnItemAdded.AddListener(HandleItemAdded);
    inventory.OnItemRemoved.AddListener(HandleItemRemoved);
}

private void OnDisable()
{
    inventory.OnItemAdded.RemoveListener(HandleItemAdded);
    inventory.OnItemRemoved.RemoveListener(HandleItemRemoved);
}

private void HandleItemAdded(ItemStack stack)
{
    Debug.Log($"Received {stack.Amount} x {stack.Data.DisplayName}");
}

private void HandleItemRemoved(ItemStack stack)
{
    Debug.Log($"Removed {stack.Amount} x {stack.Data.DisplayName}");
}
~~~

Каждый ItemSlot имеет собственное событие OnItemChanged. Используйте его для обновления одного UI-слота без перерисовки всего инвентаря.

Перемещение, split, swap и AutoStack меняют слоты и вызывают их OnItemChanged, но не вызывают глобальные OnItemAdded/OnItemRemoved, потому что общее количество предметов не меняется.

## 6. Optional Sample UI

Sample расположен в Samples~/Example/Runtime/InventoryUI.

### InventoryUIFactory

InventoryUIFactory создаёт по одному экземпляру slot prefab на каждый слот InventoryController. На prefab должен быть компонент ItemSlotView. Фабрика связывает:
- InventoryController;
- ItemSlotPresenter;
- ItemSlotView;
- drag ghost (опционально);
- ItemContextMenu (опционально).

Create() создаёт UI. Dispose() снимает подписки, очищает drag ghost, скрывает контекстное меню и уничтожает только те views, которыми владеет фабрика.

~~~csharp
var factory = new InventoryUIFactory(
    inventory,
    inventoryContainer,
    inventorySlotPrefab,
    dragGhost,
    dragGhostAmount,
    contextMenu);

factory.Create();

// При смене инвентаря или уничтожении UI:
factory.Dispose();
~~~

Не вызывайте Create после уничтожения контейнера. Передавайте валидные ссылки или используйте собственную проверку обязательных prefab-компонентов.

### ItemSlotView и ItemView

ItemSlotView реализует Unity EventSystem интерфейсы:
- IBeginDragHandler;
- IDragHandler;
- IEndDragHandler;
- IDropHandler;
- IPointerClickHandler.

Он подписывается на OnItemChanged модели ItemSlot и обновляет связанный ItemView. ItemView отображает icon и количество. В prefab слота должны быть назначены Image и TextMeshProUGUI; missingIconSprite используется как резервная иконка.

### Drag ghost

InventoryUIFactory может использовать Image для drag ghost и TMP_Text для количества. При начале drag иконка копируется из стека, позиция следует за указателем, при завершении drag ghost очищается.

### Context menu

ItemContextMenu переиспользует пул кнопок и получает IReadOnlyList<ContextMenuAction>. Действие задаёт label, callback и Interactable.

Sample ItemSlotPresenter формирует действия:
- продажа, если Sellable true и SellPrice > 0;
- разделение, если стак содержит больше одного предмета;
- выбрасывание, если Droppable true;
- уничтожение.

Действие продажи не изменяет валюту: оно удаляет стак и вызывает OnItemSold. Подписчик игры должен начислить валюту и реализовать проверки магазина. Аналогично выбрасывание удаляет предмет и вызывает OnItemDropped; создание world prefab — задача игры.

### ItemReceivedUI

Показывает иконку, имя и количество полученного предмета, ждёт displayDuration и плавно уменьшает CanvasGroup.alpha. Назначьте ссылки на Image, TMP_Text и CanvasGroup. Если CanvasGroup отсутствует, Show обновит текст/иконку, но не запустит отображение/затухание.

### PlayerInventoryUIBootstrap

Это интеграционный sample-компонент, который получает InventoryController у активного Character через проектный PlayerManager и использует GameManager для чтения UI input. Эти классы не входят в Inventory package; компонент можно заменить собственным адаптером и использовать InventoryUIFactory напрямую.

## 7. Сохранение и загрузка

Пакет не включает save/load serializer. Для сохранения не сериализуйте только имя предмета или DisplayName: они не обязаны быть уникальными и могут измениться.

Рекомендуемый DTO:
- стабильный ItemDefinition.Id;
- Amount;
- при необходимости индекс слота;
- версия схемы сохранения.

Пример структуры данных (псевдо-DTO):

~~~csharp
[Serializable]
public class SavedSlot
{
    public string ItemId;
    public int Amount;
}
~~~

При загрузке игровая система должна разрешить ItemId через каталог assets, проверить найденную ItemDefinition, проверить Amount и MaxStackSize, затем поместить предмет через SetItem/InventoryController API. Если количество больше максимального размера стака, распределите его по нескольким слотам через AddItem.

Runtime inventory сравнивает сами asset references, а не Id. Поэтому загрузчик должен разрешить Id в каноническую ссылку на ItemDefinition asset.

## 8. Ограничения и важные детали

- Один ItemSlot хранит один тип предмета.
- Стэкинг возможен только для одной и той же ссылки ItemDefinition.
- MaxStackSize должен быть минимум 1.
- ItemStack имеет публичные сериализуемые поля Data и Amount. Не записывайте в Amount отрицательные значения.
- ItemSlot.SetItem ограничивает количество вместимостью одного слота. Для добавления большого количества используйте InventoryController.AddItem, который распределяет стак по слотам.
- AddItem возвращает остаток.
- RemoveItem выполняет all-or-nothing удаление.
- RemoveFromSlot и TransferTo могут выполнять частичную операцию.
- SplitStack не может перенести весь исходный стак — используйте TryMove для полного перемещения.
- Перемещение и стэкинг генерируют slot-level OnItemChanged, но не inventory-level OnItemAdded/OnItemRemoved.
- Id не генерируется автоматически; присваивайте уникальный стабильный идентификатор.
- Clear и ClearSlot уведомляют через OnItemRemoved.
- UI sample может требовать проектные интеграции и внешние зависимости.

## 9. Диагностика

| Симптом | Возможная причина и решение |
|---|---|
| AddItem возвращает ненулевое значение | Это остаток; увеличьте количество слотов или освободите место |
| Не объединяются визуально одинаковые предметы | Стакируются только ссылки на один и тот же ItemDefinition asset |
| Можно добавить больше MaxStackSize? | Используйте InventoryController.AddItem; он распределит количество по нескольким слотам |
| RemoveItem не удаляет часть доступного количества | Метод all-or-nothing; проверьте CountItem или используйте RemoveFromSlot |
| TransferTo вернул false | Целевой инвентарь не принял ни одной единицы, источник пуст или параметры некорректны |
| UI не обновился | Проверьте ItemSlotView.Bind, OnItemChanged subscription и ссылки prefab |
| Не работает drag & drop | Проверьте EventSystem, GraphicRaycaster, Raycast Target и наличие ItemSlotView |
| Нет иконки | Назначьте Icon в ItemDefinition или missingIconSprite в ItemView |
| Не компилируется PlayerInventoryUIBootstrap | Подключите проектные Character/PlayerManager/GameManager зависимости или используйте собственный bootstrap |
| Не сохраняются предметы | Реализуйте save/load через стабильный ItemDefinition.Id; persistence не входит в пакет |

## 10. Проверка перед выпуском

Проверьте в целевом проекте Unity:
1. Создание ItemDefinition assets и сохранение Id.
2. Добавление количества, превышающего MaxStackSize, с разными размерами инвентаря.
3. Частичное заполнение совместимого стака.
4. RemoveItem при достаточном и недостаточном количестве.
5. Clear, ClearSlot и RemoveAll с проверкой событий.
6. TryMove для пустой, совместимой и несовместимой цели.
7. SplitStack при валидном и невалидном количестве.
8. TransferTo в пустой и почти заполненный инвентарь.
9. AutoStack на нескольких частичных стаках.
10. UI prefab, drag/drop, контекстное меню, переключение активного персонажа и отписку от событий.

Unity Editor/Player compilation и runtime tests должны быть выполнены в проекте с реальными UI и project dependencies. В рамках подготовки документации статическая проверка кода и JSON не заменяет запуск Unity.
