using System.Linq;
using InteractiveTerminalAPI.UI;
using InteractiveTerminalAPI.UI.Application;
using InteractiveTerminalAPI.UI.Cursor;
using InteractiveTerminalAPI.UI.Page;
using InteractiveTerminalAPI.UI.Screen;
using ShipInventoryFork.Helpers;
using ShipInventoryFork.Objects;
using static UnityEngine.InputSystem.InputAction;

namespace ShipInventoryFork.Applications;

public class ShipApplication : PageApplication
{
    #region General Texts

    private readonly string INVENTORY_TITLE = Lang.Get("INVENTORY_TITLE");

    private readonly string UNKNOWN = Lang.Get("UNKNOWN");

    #endregion
    
    public override void Initialization() => MainScreen(0);

    #region Main Screen
    
    private readonly string HOME_MESSAGE = Lang.Get("HOME_MESSAGE");
    private readonly string WELCOME_MESSAGE = Lang.Get("WELCOME_MESSAGE");

    private readonly string TRADEMARK = Lang.Get("TRADEMARK");

    protected override int GetEntriesPerPage<T>(T[] entries) => GetEntriesPerPage();
    private static int GetEntriesPerPage() => Constants.ITEMS_PER_PAGE;

    private void MainScreen(int selectedIndex)
    {
        var player = StartOfRound.Instance.localPlayerController;

        var elements = new[]
        {
            RetrieveSingleElement(),
            RetrieveTypeElement(),
            RetrieveRandomElement(),
            RetrieveAllElement(),
            InfoCursorElement(),
        };

        var optionMenu = new CursorMenu {
            cursorIndex = ItemManager.GetItems().Any() ? selectedIndex : elements.Length - 1,
            elements = elements
        };

        var screen = CreateScreen(INVENTORY_TITLE,
            [
                TextElement.Create(string.Format(
                    WELCOME_MESSAGE,
                    HUDManager.Instance.playerLevels[player.playerLevelNumber].levelName,
                    player.playerUsername
                )),
                TextElement.Create(" "),
                TextElement.Create(HOME_MESSAGE),
                TextElement.Create(" "),
                optionMenu,
            ]
        );
        currentPage = PageCursorElement.Create(0, [screen], [optionMenu]);
        SwitchScreen(screen, optionMenu, true);
    }

    #endregion

    #region Utils

    private readonly string POSITIVE_ANSWER = Lang.Get("POSITIVE_ANSWER");
    private readonly string NEGATIVE_ANSWER = Lang.Get("NEGATIVE_ANSWER");
    
    private string BoolToString(bool value) => value
        ? $"<color=green>{POSITIVE_ANSWER}</color>"
        : $"<color=red>{NEGATIVE_ANSWER}</color>";

    private System.Action<CallbackContext>? LastExitPerformedAction;

    private void RegisterExitAction(System.Action<CallbackContext> action)
    {
        UnregisterExitAction();
        LastExitPerformedAction = action;
        InteractiveTerminalAPI.Compat.InputUtils_Compat.CursorExitKey.performed -= OnScreenExit;
        InteractiveTerminalAPI.Compat.InputUtils_Compat.CursorExitKey.performed += action;
    }

    private void UnregisterExitAction()
    {
        if (LastExitPerformedAction != null)
        {
            InteractiveTerminalAPI.Compat.InputUtils_Compat.CursorExitKey.performed -= LastExitPerformedAction;
            LastExitPerformedAction = null;
        }
        // If OnScreenExit is not already registered, this is a no-op
        // Ensures OnScreenExit is never double-registered
        InteractiveTerminalAPI.Compat.InputUtils_Compat.CursorExitKey.performed -= OnScreenExit;
        InteractiveTerminalAPI.Compat.InputUtils_Compat.CursorExitKey.performed += OnScreenExit;
    }

    private BoxedScreen CreateScreen(string title, ITextElement[] elements)
    {
        UnregisterExitAction();

        return new()
        {
            Title = title,
            elements =
            [
                .. elements,
                .. ShipInventoryFork.Config.ShowTrademark.Value 
                    ? new ITextElement[] { TextElement.Create(" "), TextElement.Create(TRADEMARK) } 
                    : []
            ]
        };
    }

    private readonly string CONFIRMATION_TITLE = Lang.Get("CONFIRMATION_TITLE");
    private readonly string CONFIRMATION_MESSAGE = Lang.Get("CONFIRMATION_MESSAGE");

    private System.Action? ConfirmExitCallback;
    
    private void ConfirmElement(string message, System.Action? confirmCallback, System.Action? declineCallback = null)
    {
        // If skip confirmation, skip
        if (!ShipInventoryFork.Config.ShowConfirmation.Value)
        {
            confirmCallback?.Invoke();
            return;
        }

        ConfirmExitCallback = declineCallback;

        var optionMenu = new CursorMenu
        {
            cursorIndex = ShipInventoryFork.Config.YesPlease.Value ? 1 : 0,
            elements =
            [
                new CursorElement
                {
                    Name = NEGATIVE_ANSWER,
                    Action = () => {
                        if (declineCallback != null) {
                            declineCallback.Invoke();
                        } else {
                            MainScreen(0);
                        }
                    }
                },
                new CursorElement
                {
                    Name = POSITIVE_ANSWER,
                    Action = () => confirmCallback?.Invoke()
                }
            ]
        };

        var screen = CreateScreen(CONFIRMATION_TITLE,
            [
                TextElement.Create(message),
                TextElement.Create(" "),
                TextElement.Create(CONFIRMATION_MESSAGE),
                TextElement.Create(" "),
                optionMenu
            ]
        );
        currentPage = PageCursorElement.Create(0, [screen], [optionMenu]);
        SwitchScreen(screen, optionMenu, true);

        RegisterExitAction(OnConfirmExit);
    }

    private void OnConfirmExit(CallbackContext context)
    {
        ConfirmExitCallback?.Invoke();
    }

    #endregion

    #region Info Screen

    private readonly string SHIP_INFO = Lang.Get("SHIP_INFO");
    
    private readonly string STATUS_TITLE = Lang.Get("STATUS_TITLE");

    private readonly string SHIP_INFO_TOTAL = Lang.Get("SHIP_INFO_TOTAL");
    private readonly string SHIP_INFO_COUNT = Lang.Get("SHIP_INFO_COUNT");
    private readonly string SHIP_INFO_KEEP_HEADER = Lang.Get("SHIP_INFO_KEEP_HEADER");
    private readonly string SHIP_INFO_KEEP_ON_WIPE = Lang.Get("SHIP_INFO_KEEP_ON_WIPE");
    private readonly string SHIP_INFO_KEEP_ON_FIRE = Lang.Get("SHIP_INFO_KEEP_ON_FIRE");
    private readonly string SHIP_INFO_IN_ORBIT = Lang.Get("SHIP_INFO_IN_ORBIT");
    
    private CursorElement InfoCursorElement() => new()
    {
        Name = SHIP_INFO,
        Action = GetInfo
    };
    
    private void GetInfo()
    {
        var options = new CursorMenu
        {
            cursorIndex = 0,
            elements = []
        };
        var screen = CreateScreen(STATUS_TITLE,
            [
                TextElement.Create(string.Format(SHIP_INFO_TOTAL, ItemManager.GetTotalValue())),
                TextElement.Create(string.Format(
                    SHIP_INFO_COUNT,
                    ItemManager.GetItems().Count(),
                    ShipInventoryFork.Config.MaxItemCount.Value
                )),
                TextElement.Create(" "),
                TextElement.Create(string.Format(SHIP_INFO_IN_ORBIT, BoolToString(!ShipInventoryFork.Config.RequireInOrbit.Value))),
                TextElement.Create(" "),
                TextElement.Create(SHIP_INFO_KEEP_HEADER),
                TextElement.Create(string.Format(SHIP_INFO_KEEP_ON_WIPE, BoolToString(ShipInventoryFork.Config.ActAsSafe.Value))),
                TextElement.Create(string.Format(SHIP_INFO_KEEP_ON_FIRE, BoolToString(ShipInventoryFork.Config.PersistThroughFire.Value))),
            ]
        );
        
        currentPage = PageCursorElement.Create(0, [screen], [options]);
        SwitchScreen(screen, options, true);

        RegisterExitAction(OnInfoExit);
    }

    private void OnInfoExit(CallbackContext context) => MainScreen(4);

    #endregion
    
    #region Retrieve Single Screen

    private readonly string SINGLE_RETRIEVE = Lang.Get("SINGLE_RETRIEVE");
    private readonly string TEXT_SINGLE_RETRIEVE = Lang.Get("TEXT_SINGLE_RETRIEVE");
    private readonly string SINGLE_RETRIEVE_MESSAGE = Lang.Get("SINGLE_RETRIEVE_MESSAGE");
    private readonly string SINGLE_ITEM_FORMAT = Lang.Get("SINGLE_ITEM_FORMAT");
    
    private CursorElement RetrieveSingleElement() => new()
    {
        Name = SINGLE_RETRIEVE,
        Active = _ => ItemManager.GetItems().Any(),
        SelectInactive = false,
        Action = () => RetrieveSingle()
    };

    private CursorElement RenderSingle(ItemData itemData, bool onlyGroup, int index)
    {
        var item = itemData.GetItem();
        string name = UNKNOWN;
        if (item is not null)
        {
            name = string.Format(
                SINGLE_ITEM_FORMAT,
                item.itemName,
                itemData.SCRAP_VALUE
            );
        }

        var element = new CursorElement
        {
            Name = name,
            Action = () =>
            {
                string message = TEXT_SINGLE_RETRIEVE;

                message = string.Format(message, name);
                ConfirmElement(message, () =>
                {
                    ChuteInteract.Instance?.SpawnItemServerRpc(itemData);
                    if (onlyGroup)
                        MainScreen(0);
                    else
                        RetrieveSingle(index);
                }, () => RetrieveSingle(index));
            }
        };
        
        return element;
    }
    
    private void RetrieveSingle(int selectedIndex = 0)
    {
        var ENTRIES_PER_PAGE = GetEntriesPerPage();

        var items = ItemManager.GetItems();
        int cursorCount = items.Count();
        (ItemData[][] pageGroups, CursorMenu[] cursorMenus, IScreen[] screens) = GetPageEntries(items.ToArray());

        for (int i = 0; i < pageGroups.Length; i++)
        {
            var elements = new CursorElement[pageGroups[i].Length];
            for (int j = 0; j < elements.Length; j++)
            {
                var itemData = pageGroups[i][j];
                if (itemData.Equals(default(ItemData)))
                    // It's normal to have default entries (the array is page-sized, but there may be fewer entries)
                    continue;

                elements[j] = RenderSingle(itemData, cursorCount == 1, i * ENTRIES_PER_PAGE + j);
            }

            cursorMenus[i] = new CursorMenu
            {
                cursorIndex = 0,
                elements = elements
            };
            screens[i] = CreateScreen(INVENTORY_TITLE,
                [
                    TextElement.Create(SINGLE_RETRIEVE_MESSAGE),
                    TextElement.Create(" "),
                    cursorMenus[i],
                ]
            );
        }

        if (selectedIndex >= cursorCount)
        {
            selectedIndex = cursorCount - 1;
        }

        int currentPageIndex = selectedIndex / ENTRIES_PER_PAGE;
        int currentCursorIndex = selectedIndex % ENTRIES_PER_PAGE;

        // Set the current page's cursor
        cursorMenus[currentPageIndex].cursorIndex = currentCursorIndex;

        currentPage = PageCursorElement.Create(currentPageIndex, screens, cursorMenus);
        currentCursorMenu = currentPage.GetCurrentCursorMenu();
        currentScreen = currentPage.GetCurrentScreen();

        RegisterExitAction(OnRetrieveSingleExit);
    }

    private void OnRetrieveSingleExit(CallbackContext context) => MainScreen(0);

    #endregion

    #region Retrieve Type Screen

    private readonly string TYPE_RETRIEVE = Lang.Get("TYPE_RETRIEVE");
    private readonly string TEXT_TYPE_RETRIEVE = Lang.Get("TEXT_TYPE_RETRIEVE");
    private readonly string TYPE_RETRIEVE_MESSAGE = Lang.Get("TYPE_RETRIEVE_MESSAGE");
    private readonly string TYPE_ITEM_FORMAT = Lang.Get("TYPE_ITEM_FORMAT");
    
    private CursorElement RetrieveTypeElement() => new()
    {
        Name = TYPE_RETRIEVE,
        Active = _ => ItemManager.GetItems().Any(),
        SelectInactive = false,
        Action = () => RetrieveType()
    };

    private CursorElement RenderType(IGrouping<string, ItemData> group, bool onlyGroup, int index)
    {
        var item = group.First().GetItem();
        var amount = group.Count();

        string name = UNKNOWN;
        if (item is not null)
        {
            name = string.Format(
                TYPE_ITEM_FORMAT,
                item.itemName,
                amount,
                group.Sum(data => data.SCRAP_VALUE)
            );
        }

        var element = new CursorElement
        {
            Name = name,
            Action = () =>
            {
                string message = TEXT_TYPE_RETRIEVE;

                message = string.Format(message, amount, item?.itemName ?? UNKNOWN);
                ConfirmElement(message, () =>
                {
                    ChuteInteract.Instance?.SpawnItemServerRpc(
                        group.First(),
                        amount
                    );
                    
                    if (onlyGroup)
                        MainScreen(1);
                    else
                        RetrieveType(index);
                }, () => RetrieveType(index));
            }
        };
        
        return element;
    }

    private void RetrieveType(int selectedIndex = 0)
    {
        var ENTRIES_PER_PAGE = GetEntriesPerPage();
        
        var types = ItemManager.GetItems().GroupBy(i => i.ID);
        int cursorCount = types.Count();
        (IGrouping<string, ItemData>[][] pageGroups, CursorMenu[] cursorMenus, IScreen[] screens) = GetPageEntries(types.ToArray());

        for (int i = 0; i < pageGroups.Length; i++)
        {
            var elements = new CursorElement[pageGroups[i].Length];
            for (int j = 0; j < elements.Length; j++)
            {
                var group = pageGroups[i][j];
                
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (group == null)
                    // It's normal to have null entries (the array is page-sized, but there may be fewer entries)
                    continue;
                
                elements[j] = RenderType(group, cursorCount == 1, i * ENTRIES_PER_PAGE + j);
            }

            cursorMenus[i] = new CursorMenu
            {
                cursorIndex = 0,
                elements = elements
            };
            screens[i] = CreateScreen(INVENTORY_TITLE,
                [
                    TextElement.Create(TYPE_RETRIEVE_MESSAGE),
                    TextElement.Create(" "),
                    cursorMenus[i]
                ]
            );
        }

        if (selectedIndex >= cursorCount)
        {
            selectedIndex = cursorCount - 1;
        }

        int currentPageIndex = selectedIndex / ENTRIES_PER_PAGE;
        int currentCursorIndex = selectedIndex % ENTRIES_PER_PAGE;

        // Set the current page's cursor
        cursorMenus[currentPageIndex].cursorIndex = currentCursorIndex;

        currentPage = PageCursorElement.Create(currentPageIndex, screens, cursorMenus);
        currentCursorMenu = currentPage.GetCurrentCursorMenu();
        currentScreen = currentPage.GetCurrentScreen();

        RegisterExitAction(OnRetrieveTypeExit);
    }

    private void OnRetrieveTypeExit(CallbackContext context) => MainScreen(1);

    #endregion

    #region Retrieve Random Screen

    private readonly string RANDOM_RETRIEVE = Lang.Get("RANDOM_RETRIEVE");
    private readonly string TEXT_RANDOM_RETRIEVE = Lang.Get("TEXT_RANDOM_RETRIEVE");
    
    private CursorElement RetrieveRandomElement() => new()
    {
        Name = RANDOM_RETRIEVE,
        Active = _ => ItemManager.GetItems().Any(),
        SelectInactive = false,
        Action = RetrieveRandom
    };
    
    private void RetrieveRandom()
    {
        // Random object
        var items = ItemManager.GetItems();

        object randomObj = items.ElementAt(UnityEngine.Random.Range(0, items.Count()));
        
        // Generate message
        string message = TEXT_RANDOM_RETRIEVE;

        string name = randomObj switch
        {
            ItemData data => data.GetItem()?.itemName,
            EnemyType enemyType => enemyType.enemyName,
            _ => null
        } ?? UNKNOWN;

        message = string.Format(message, name);
        
        ConfirmElement(message, () =>
        {
            // Spawn random
            if (randomObj is ItemData data)
                ChuteInteract.Instance?.SpawnItemServerRpc(data);

            MainScreen(2);
        }, () => MainScreen(2));
    }

    #endregion

    #region Retrieve All Screen

    private readonly string ALL_RETRIEVE = Lang.Get("ALL_RETRIEVE");
    private readonly string TEXT_ALL_RETRIEVE = Lang.Get("TEXT_ALL_RETRIEVE");
    
    private CursorElement RetrieveAllElement() => new()
    {
        Name = ALL_RETRIEVE,
        Active = _ => ItemManager.GetItems().Any(),
        SelectInactive = false,
        Action = RetrieveAll
    };
    
    private void RetrieveAll()
    {
        string text = string.Format(
            TEXT_ALL_RETRIEVE,
            ItemManager.GetItems().Sum(i => i.SCRAP_VALUE)
        );
        
        ConfirmElement(text, () =>
        {
            foreach (var group in ItemManager.GetItems().GroupBy(i => i.ID))
            {
                ChuteInteract.Instance?.SpawnItemServerRpc(
                    group.First(),
                    group.Count()
                );
            }
            
            MainScreen(3);
        }, () => MainScreen(3));
    }

    #endregion
}