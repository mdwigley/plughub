using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using PlugHub.Plugin.Controls.Interfaces.Controls;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PlugHub.Plugin.Controls.Controls
{
    public enum ContentDeckDisplayMode
    {
        Tab,
        Deck
    }
    public enum ContentDeckActivationMode
    {
        Transient,
        Persistent
    }
    public enum ContentDeckEmptyBehavior
    {
        Collapse,
        Visible
    }

    public class ItemsReorderedEventArgs(RoutedEvent routedEvent, IContentItem item, int oldIndex, int newIndex)
        : RoutedEventArgs(routedEvent)
    {
        public IContentItem Item { get; } = item;
        public int OldIndex { get; } = oldIndex;
        public int NewIndex { get; } = newIndex;
    }

    public class ContentDeckItem : ContentPresenter
    {
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectingItemsControl.IsSelectedProperty)
            {
                bool isSelected = change.GetNewValue<bool>();

                this.PseudoClasses.Set(":selected", isSelected);
            }
        }
    }

    public class ContentDeck : SelectingItemsControl
    {
        private EventHandler<PointerPressedEventArgs>? defocusHandler;

        private TopLevel? top;

        private Point? clickPosition;
        private bool isMoving;
        private RotationView? pressedRot;
        private IContentItem? pressedItem;
        private IContentItem? lastTargetItem;
        private Rect? lastTargetBounds;
        private Control? activeSource;
        private int lastReorderOldIndex = -1;
        private int lastReorderNewIndex = -1;

        private IContentItem? activeItem;

        private readonly Grid deckGrid = new()
        {
            Margin = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
            RoutedEvent.Register<ContentDeck, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? Opened
        {
            add => this.AddHandler(OpenedEvent, value);
            remove => this.RemoveHandler(OpenedEvent, value);
        }

        public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
            RoutedEvent.Register<ContentDeck, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);
        public event EventHandler<RoutedEventArgs>? Closed
        {
            add => this.AddHandler(ClosedEvent, value);
            remove => this.RemoveHandler(ClosedEvent, value);
        }

        public static readonly RoutedEvent<ItemsReorderedEventArgs> ItemsReorderedEvent =
            RoutedEvent.Register<ContentDeck, ItemsReorderedEventArgs>(nameof(ItemsReordered), RoutingStrategies.Bubble);
        public event EventHandler<ItemsReorderedEventArgs>? ItemsReordered
        {
            add => this.AddHandler(ItemsReorderedEvent, value);
            remove => this.RemoveHandler(ItemsReorderedEvent, value);
        }

        #region DockGutter: Style Properties

        public static readonly StyledProperty<Dock> DockEdgeProperty =
            AvaloniaProperty.Register<ContentDeck, Dock>(nameof(DockEdge), Dock.Left);
        public Dock DockEdge
        {
            get => this.GetValue(DockEdgeProperty);
            set => this.SetValue(DockEdgeProperty, value);
        }

        public static readonly StyledProperty<ContentDeckDisplayMode> DisplayModeProperty =
            AvaloniaProperty.Register<ContentDeck, ContentDeckDisplayMode>(nameof(DisplayMode), ContentDeckDisplayMode.Tab);
        public ContentDeckDisplayMode DisplayMode
        {
            get => this.GetValue(DisplayModeProperty);
            set => this.SetValue(DisplayModeProperty, value);
        }

        public static readonly StyledProperty<Orientation> DeckOrientationProperty =
            AvaloniaProperty.Register<ContentDeck, Orientation>(nameof(DeckOrientation), Orientation.Vertical);
        public Orientation DeckOrientation
        {
            get => this.GetValue(DeckOrientationProperty);
            set => this.SetValue(DeckOrientationProperty, value);
        }

        public static readonly StyledProperty<ContentDeckActivationMode> ActivationModeProperty =
            AvaloniaProperty.Register<ContentDeck, ContentDeckActivationMode>(nameof(ActivationMode), ContentDeckActivationMode.Transient);
        public ContentDeckActivationMode ActivationMode
        {
            get => this.GetValue(ActivationModeProperty);
            set => this.SetValue(ActivationModeProperty, value);
        }

        public static readonly StyledProperty<ContentDeckEmptyBehavior> EmptyBehaviorProperty =
            AvaloniaProperty.Register<ContentDeck, ContentDeckEmptyBehavior>(nameof(ContentDeckEmptyBehavior), ContentDeckEmptyBehavior.Collapse);
        public ContentDeckEmptyBehavior EmptyBehavior
        {
            get => this.GetValue(EmptyBehaviorProperty);
            set => this.SetValue(EmptyBehaviorProperty, value);
        }

        public static readonly StyledProperty<AvaloniaList<GridLength>> ContentSizesProperty =
            AvaloniaProperty.Register<ContentDeck, AvaloniaList<GridLength>>(nameof(ContentSizes), []);
        public AvaloniaList<GridLength> ContentSizes
        {
            get => this.GetValue(ContentSizesProperty);
            set => this.SetValue(ContentSizesProperty, value);
        }

        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<ContentDeck, double>(nameof(Spacing), 5);
        public double Spacing
        {
            get => this.GetValue(SpacingProperty);
            set => this.SetValue(SpacingProperty, value);
        }

        public static readonly StyledProperty<GridLength> MinContentSizeProperty =
            AvaloniaProperty.Register<ContentDeck, GridLength>(nameof(MinContentSize), new GridLength(32, GridUnitType.Star));
        public GridLength MinContentSize
        {
            get => this.GetValue(MinContentSizeProperty);
            set => this.SetValue(MinContentSizeProperty, value);
        }

        public static readonly StyledProperty<IBrush?> AccentBrushProperty =
            AvaloniaProperty.Register<ContentDeck, IBrush?>(nameof(AccentBrush));
        public IBrush? AccentBrush
        {
            get => this.GetValue(AccentBrushProperty);
            set => this.SetValue(AccentBrushProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate?> ContentItemTemplateProperty =
            AvaloniaProperty.Register<ContentDeck, IDataTemplate?>(nameof(ContentItemTemplate));
        public IDataTemplate? ContentItemTemplate
        {
            get => this.GetValue(ContentItemTemplateProperty);
            set => this.SetValue(ContentItemTemplateProperty, value);
        }

        #endregion

        #region DockGutter: Direct Properties

        public static readonly DirectProperty<ContentDeck, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, bool>(nameof(IsOpen), o => o.IsOpen, (o, v) => o.IsOpen = v);
        private bool isOpen;
        public bool IsOpen
        {
            get => this.isOpen;
            set
            {
                if (this.SetAndRaise(IsOpenProperty, ref this.isOpen, value))
                {
                    if (value) this.RaiseEvent(new RoutedEventArgs(OpenedEvent));
                    else this.RaiseEvent(new RoutedEventArgs(ClosedEvent));
                }
            }
        }

        public static readonly DirectProperty<ContentDeck, Orientation> OrientationProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, Orientation>(nameof(Orientation), o => o.Orientation);
        private Orientation orientation;
        public Orientation Orientation
        {
            get => this.orientation;
            private set => this.SetAndRaise(OrientationProperty, ref this.orientation, value);
        }

        public static readonly DirectProperty<ContentDeck, double> RotationProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, double>(nameof(Rotation), o => o.Rotation, (o, v) => o.Rotation = v, 0);
        private double rotation;
        public double Rotation
        {
            get => this.rotation;
            private set => this.SetAndRaise(RotationProperty, ref this.rotation, value);
        }

        public static readonly DirectProperty<ContentDeck, object?> ActiveContentProperty =
            AvaloniaProperty.RegisterDirect<ContentDeck, object?>(nameof(ActiveContent), o => o.ActiveContent, (o, v) => o.ActiveContent = v);
        private object? activeContent;
        public object? ActiveContent
        {
            get => this.activeContent;
            set => this.SetAndRaise(ActiveContentProperty, ref this.activeContent, value);
        }

        #endregion

        private bool IsVertical
        {
            get => this.DeckOrientation == Orientation.Vertical;
        }

        public ContentDeck()
        {
            this.Items.CollectionChanged += this.OnCollectionChanged;
            this.SelectionMode = SelectionMode.AlwaysSelected;
            this.activeItem = null;
            this.activeContent = null;
            this.IsOpen = this.ActivationMode == ContentDeckActivationMode.Persistent;
            this.ActiveContent = this.deckGrid;

            ItemsSourceProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                if (e.OldValue is ObservableCollection<IContentItem> oldColl)
                    oldColl.CollectionChanged -= x.OnCollectionChanged;
                if (e.NewValue is ObservableCollection<IContentItem> newColl)
                    newColl.CollectionChanged += x.OnCollectionChanged;

                x.UpdateVisibility();
            });
            DockEdgeProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                x.UpdateOrientation();
            });
            DisplayModeProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                x.UpdateGridVisibility();
            });
            ActivationModeProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                x.IsOpen = ((ContentDeckActivationMode?)e.NewValue) == ContentDeckActivationMode.Persistent;
            });
            ContentSizesProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                x.UpdateGridVisibility();
            });
            SelectedIndexProperty.Changed.AddClassHandler<ContentDeck>((x, e) =>
            {
                int index = x.SelectedIndex;

                if (index >= 0 && index < x.Items.Count)
                    x.activeItem = x.Items[index] as IContentItem;
                else
                    x.activeItem = null;

                x.UpdateGridVisibility();
            });
        }

        #region DockGutter: Internal Upkeep

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.UpdateOrientation();
            this.UpdateVisibility();

            this.HookClicks();
            this.HookSwitchDisplay(e);
            this.HookDefocusClose();
            this.HookScoller(e);

            this.NormalizeContentSizes();
            this.RebuildGrid();
        }

        protected virtual void UpdateOrientation()
        {
            bool isLeftOrRight = this.DockEdge == Dock.Left || this.DockEdge == Dock.Right;

            this.Rotation = isLeftOrRight ? 90 : 0;

            this.Orientation = isLeftOrRight
                ? Orientation.Vertical
                : Orientation.Horizontal;

            this.PseudoClasses.Set(":edge-left", this.DockEdge == Dock.Left);
            this.PseudoClasses.Set(":edge-right", this.DockEdge == Dock.Right);
            this.PseudoClasses.Set(":edge-top", this.DockEdge == Dock.Top);
            this.PseudoClasses.Set(":edge-bottom", this.DockEdge == Dock.Bottom);
        }
        protected virtual void UpdateVisibility()
        {
            this.IsVisible = this.EmptyBehavior == ContentDeckEmptyBehavior.Visible || this.Items.Count > 0;
        }

        protected virtual void HookClicks()
        {
            this.RemoveHandler(PointerPressedEvent, this.OnItemPressed);
            this.RemoveHandler(PointerMovedEvent, this.OnItemMoved);
            this.RemoveHandler(PointerReleasedEvent, this.OnItemReleased);

            this.AddHandler(PointerPressedEvent, this.OnItemPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AddHandler(PointerMovedEvent, this.OnItemMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AddHandler(PointerReleasedEvent, this.OnItemReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }
        protected virtual void HookSwitchDisplay(TemplateAppliedEventArgs e)
        {
            Button? transferButton = e.NameScope.Find<Button>("PART_TransferButton");

            if (transferButton != null)
            {
                transferButton.Click += (s, e) =>
                {
                    if (this.DisplayMode == ContentDeckDisplayMode.Deck)
                        this.DisplayMode = ContentDeckDisplayMode.Tab;
                    else
                        this.DisplayMode = ContentDeckDisplayMode.Deck;
                };
            }
        }
        protected virtual void HookDefocusClose()
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);

            if (topLevel == null) return;

            if (this.top != null && this.defocusHandler != null)
                this.top.RemoveHandler(PointerPressedEvent, this.defocusHandler);

            this.top = topLevel;

            this.defocusHandler = (s, ev) =>
            {
                if (!this.IsOpen)
                    return;

                if (ev.Source is Visual v)
                {
                    if (this.activeSource is Visual btn && (ReferenceEquals(v, btn) || btn.IsVisualAncestorOf(v)))
                        return;

                    if (this.ActiveContent is Visual content)
                    {
                        ResizeBox? host = content.FindAncestorOfType<ResizeBox>(includeSelf: true);
                        if (host != null && (ReferenceEquals(v, host) || host.IsVisualAncestorOf(v)))
                            return;
                    }

                    if (this.IsVisualAncestorOf(v))
                        return;
                }

                this.IsOpen = false;
            };

            this.top.AddHandler(PointerPressedEvent, this.defocusHandler, RoutingStrategies.Tunnel);
        }
        protected virtual void HookScoller(TemplateAppliedEventArgs e)
        {
            ScrollViewer? scroller = e.NameScope.Find<ScrollViewer>("PART_ContentItemScroller");

            scroller?.AddHandler(PointerWheelChangedEvent, this.OnPointerWheelChanged, RoutingStrategies.Tunnel);
        }

        #endregion

        #region DockGutter: Event Hanlders

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
            => new ContentDeckItem();

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            this.HookDefocusClose();
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (this.top != null && this.defocusHandler != null)
                this.top.RemoveHandler(PointerPressedEvent, this.defocusHandler);

            this.defocusHandler = null;
            this.top = null;

            base.OnDetachedFromVisualTree(e);
        }
        protected virtual void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                if (this.Orientation == Orientation.Horizontal)
                {
                    double delta = e.Delta.Y;

                    sv.Offset = new Vector(Math.Max(0, sv.Offset.X + delta * -40), sv.Offset.Y);

                    e.Handled = true;
                }
            }
        }
        protected virtual void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    this.RebuildGrid();

                    bool isTabMode = this.DisplayMode == ContentDeckDisplayMode.Tab;
                    bool noActiveItem = this.activeItem == null;
                    bool hasItems = this.Items.Count > 0;
                    bool hasNewItems = e.NewItems?.Count > 0;

                    if (isTabMode && noActiveItem && hasItems && hasNewItems)
                    {
                        int lastNewIndex = e.NewStartingIndex + e.NewItems!.Count - 1;

                        if (this.Items[lastNewIndex] is IContentItem newItem)
                            this.SetActiveItem(newItem);
                        else
                            this.ClearActiveItem();
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:

                    foreach (IContentItem s in e.OldItems?.OfType<IContentItem>() ?? [])
                        if (s.Content != null)
                            this.deckGrid.Children.Remove(s.Content);

                    this.RebuildGrid();

                    bool noItemsLeft = this.Items.Count == 0;
                    bool activeItemRemoved = this.activeItem != null && !this.Items.Contains(this.activeItem);

                    if (noItemsLeft)
                    {
                        this.ClearActiveItem();
                    }
                    else if (activeItemRemoved)
                    {
                        int candidateIndex = e.OldStartingIndex;

                        if (candidateIndex >= this.Items.Count)
                            candidateIndex = this.Items.Count - 1;

                        if (this.Items[candidateIndex] is IContentItem candidate)
                            this.SetActiveItem(candidate);
                        else
                            this.ClearActiveItem();
                    }

                    if (this.ActivationMode == ContentDeckActivationMode.Transient)
                        this.IsOpen = false;

                    break;

                case NotifyCollectionChangedAction.Replace:
                    this.RebuildGrid();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.ClearDeckGrid();
                    break;
            }

            this.UpdateVisibility();
        }
        protected virtual void OnSplitterDragCompleted(int gridIndex)
        {
            int prevIndex = (gridIndex - 1) / 2;
            int nextIndex = (gridIndex + 1) / 2;

            if (prevIndex >= 0 && nextIndex < this.ContentSizes.Count)
            {
                if (this.IsVertical)
                {
                    RowDefinition prevRow = this.deckGrid.RowDefinitions[gridIndex - 1];
                    RowDefinition nextRow = this.deckGrid.RowDefinitions[gridIndex + 1];

                    this.ContentSizes[prevIndex] = prevRow.Height;
                    this.ContentSizes[nextIndex] = nextRow.Height;
                }
                else
                {
                    ColumnDefinition prevCol = this.deckGrid.ColumnDefinitions[gridIndex - 1];
                    ColumnDefinition nextCol = this.deckGrid.ColumnDefinitions[gridIndex + 1];

                    this.ContentSizes[prevIndex] = prevCol.Width;
                    this.ContentSizes[nextIndex] = nextCol.Width;
                }
            }
        }

        #endregion

        #region DockGutter: Gutter Item Reorder

        protected virtual void OnItemPressed(object? sender, PointerPressedEventArgs e)
        {
            this.pressedRot = null;
            this.pressedItem = null;
            this.activeSource = null;

            if (e.Source is Visual v)
            {
                RotationView? rot = v.FindAncestorOfType<RotationView>(includeSelf: true);

                if (rot?.DataContext is IContentItem d)
                {
                    this.pressedRot = rot;
                    this.activeSource = rot;
                    this.pressedItem = d;
                }
            }

            this.clickPosition = e.GetPosition(this);
            this.isMoving = false;
        }
        protected virtual void OnItemMoved(object? sender, PointerEventArgs e)
        {
            if (this.clickPosition == null || this.pressedRot == null || this.pressedItem == null)
                return;

            Point current = e.GetPosition(this);

            double dx = current.X - this.clickPosition.Value.X;
            double dy = current.Y - this.clickPosition.Value.Y;
            double distSq = dx * dx + dy * dy;

            bool beyondThreshold = distSq > 4 * 4;

            if (!beyondThreshold)
                return;

            if (!this.isMoving)
            {
                this.Cursor = new Cursor(StandardCursorType.DragMove);

                e.Pointer.Capture(this);
            }

            this.isMoving = true;
            this.OnItemReorder(this.pressedItem, current);
        }
        protected virtual void OnItemReorder(IContentItem pressedState, Point currentPos)
        {
            if (this.ItemsSource == null)
                return;

            int currentIndex = this.Items.IndexOf(pressedState);

            if (currentIndex < 0)
                return;

            IContentItem? targetState = null;
            Rect? targetBounds = null;

            for (int i = 0; i < this.Items.Count; i++)
            {
                if (this.ContainerFromIndex(i) is Control container)
                {
                    Point? topLeft = container.TranslatePoint(new Point(0, 0), this);

                    if (topLeft == null) continue;

                    Rect rect = new(topLeft.Value, container.Bounds.Size);

                    if (this.Orientation == Orientation.Vertical)
                    {
                        if (currentPos.Y >= rect.Top && currentPos.Y <= rect.Bottom)
                        {
                            targetState = this.Items[i] as IContentItem;
                            targetBounds = rect;

                            break;
                        }
                    }
                    else
                    {
                        if (currentPos.X >= rect.Left && currentPos.X <= rect.Right)
                        {
                            targetState = this.Items[i] as IContentItem;
                            targetBounds = rect;

                            break;
                        }
                    }
                }
            }

            if (this.lastTargetItem != null && this.lastTargetBounds.HasValue &&
                this.lastTargetBounds.Value.Contains(currentPos))
            {
                return;
            }

            if (this.lastTargetItem != null && this.lastTargetBounds.HasValue &&
                !this.lastTargetBounds.Value.Contains(currentPos))
            {
                this.lastTargetItem = null;
                this.lastTargetBounds = null;
            }

            if (targetState == null || ReferenceEquals(targetState, pressedState))
                return;

            if (!ReferenceEquals(targetState, this.lastTargetItem))
            {
                int targetIndex = this.Items.IndexOf(targetState);
                int currentIndexNow = this.Items.IndexOf(pressedState);

                if (targetIndex >= 0 && targetIndex != currentIndexNow)
                {
                    if (this.ItemsSource is IList list)
                    {
                        list.RemoveAt(currentIndexNow);
                        list.Insert(targetIndex, pressedState);

                        this.lastReorderOldIndex = currentIndexNow;
                        this.lastReorderNewIndex = targetIndex;

                        this.SetActiveItem(pressedState);
                    }
                }

                this.lastTargetItem = targetState;
                this.lastTargetBounds = targetBounds;
            }
        }
        protected virtual void OnItemReleased(object? sender, RoutedEventArgs e)
        {
            if (this.pressedItem != null && this.pressedRot != null)
            {
                Point? topLeft = this.pressedRot.TranslatePoint(new Point(0, 0), this);

                if (topLeft != null)
                {
                    Rect bounds = new(topLeft.Value, this.pressedRot.Bounds.Size);
                    Point releasePos = (e as PointerReleasedEventArgs)?.GetPosition(this) ?? default;

                    if (!bounds.Contains(releasePos))
                    {
                        this.ResetPointerState();

                        return;
                    }
                }

                if (!this.isMoving)
                {
                    if (this.ActivationMode == ContentDeckActivationMode.Transient)
                        this.TogglePanel(this.pressedItem);

                    this.SetActiveItem(this.pressedItem);
                }
                else
                {
                    this.SelectedItem = this.pressedItem;

                    this.RaiseEvent(
                        new ItemsReorderedEventArgs(
                            ItemsReorderedEvent,
                            this.pressedItem!,
                            this.lastReorderOldIndex,
                            this.lastReorderNewIndex));
                }

                e.Handled = true;
            }

            this.ResetPointerState();
        }

        protected virtual void ResetPointerState()
        {
            this.clickPosition = null;
            this.isMoving = false;
            this.pressedRot = null;
            this.pressedItem = null;
            this.activeSource = null;
            this.Cursor = new Cursor(StandardCursorType.Arrow);
            this.lastTargetItem = null;
            this.lastTargetBounds = null;
            this.lastReorderOldIndex = -1;
            this.lastReorderNewIndex = -1;
        }

        #endregion

        #region DockGutter: Panel Controlers

        protected virtual void SetActiveItem(IContentItem item)
        {
            this.activeItem = item;
            this.SelectedItem = item;
            if (this.DisplayMode == ContentDeckDisplayMode.Tab)
                this.UpdateGridVisibility();
        }

        protected virtual void ClearActiveItem()
        {
            this.activeItem = null;
            this.SelectedItem = null;
            if (this.DisplayMode == ContentDeckDisplayMode.Tab)
                this.UpdateGridVisibility();
        }

        protected virtual void TogglePanel(IContentItem? previous)
        {
            if (this.IsOpen && this.activeItem == previous)
            {
                this.IsOpen = false;

                return;
            }

            this.IsOpen = this.activeItem != null;
        }

        #endregion

        #region DockGutter: Grid Handlers

        protected virtual GridLength GetLengthOrMin(int slotIndex)
        {
            if (slotIndex < this.ContentSizes.Count)
            {
                GridLength candidate = this.ContentSizes[slotIndex];

                return candidate.Value < this.MinContentSize.Value
                    ? this.MinContentSize
                    : candidate;
            }

            return this.MinContentSize;
        }
        protected virtual void NormalizeContentSizes()
        {
            int required = this.Items.OfType<IContentItem>().Count(x => x.Content != null);

            this.ContentSizes ??= [];

            while (this.ContentSizes.Count > required)
                this.ContentSizes.RemoveAt(this.ContentSizes.Count - 1);

            while (this.ContentSizes.Count < required)
                this.ContentSizes.Add(this.MinContentSize);

            for (int i = 0; i < this.ContentSizes.Count; i++)
            {
                GridLength g = this.ContentSizes[i];

                if (g.IsStar && this.MinContentSize.IsStar && g.Value < this.MinContentSize.Value)
                    this.ContentSizes[i] = this.MinContentSize;
                else if (!g.IsStar && this.MinContentSize.IsAbsolute && g.Value < this.MinContentSize.Value)
                    this.ContentSizes[i] = this.MinContentSize;
            }
        }
        protected virtual GridSplitter CreateGridSplitter(int gridIndex)
        {
            GridSplitter splitter = new()
            {
                ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                Background = Brushes.Transparent,
                ResizeDirection = this.IsVertical ? GridResizeDirection.Rows : GridResizeDirection.Columns,
            };

            if (this.IsVertical)
            {
                splitter.Height = this.Spacing;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;

                Grid.SetRow(splitter, gridIndex);
            }
            else
            {
                splitter.Width = this.Spacing;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;

                Grid.SetColumn(splitter, gridIndex);
            }

            splitter.DragCompleted += (_, __) =>
            {
                this.NormalizeContentSizes();
                this.OnSplitterDragCompleted(gridIndex);
            };

            return splitter;
        }

        protected virtual void UpdateGridVisibility()
        {
            if (this.DisplayMode == ContentDeckDisplayMode.Deck)
                this.UpdateDeckVisibility();
            else
                this.UpdateTabVisibility();
        }
        protected virtual void UpdateDeckVisibility()
        {
            int count = this.IsVertical
                ? this.deckGrid.RowDefinitions.Count
                : this.deckGrid.ColumnDefinitions.Count;

            for (int i = 0; i < count; i++)
            {
                bool isContent = i % 2 == 0;

                int slot = i / 2;

                if (isContent)
                {
                    GridLength length = this.GetLengthOrMin(slot);

                    if (this.IsVertical)
                    {
                        RowDefinition row = this.deckGrid.RowDefinitions[i];
                        row.Height = length;
                        row.MinHeight = this.MinContentSize.Value;
                    }
                    else
                    {
                        ColumnDefinition col = this.deckGrid.ColumnDefinitions[i];
                        col.Width = length;
                        col.MinWidth = this.MinContentSize.Value;
                    }
                }
                else
                {
                    if (this.IsVertical)
                        this.deckGrid.RowDefinitions[i].Height = new GridLength(this.Spacing);
                    else
                        this.deckGrid.ColumnDefinitions[i].Width = new GridLength(this.Spacing);
                }
            }

            foreach (Control child in this.deckGrid.Children.OfType<Control>())
                child.IsVisible = true;
        }
        protected virtual void UpdateTabVisibility()
        {
            int activeIndex = this.Items
                .OfType<IContentItem>()
                .Select((item, i) => new { item, i })
                .FirstOrDefault(x => x.item == this.activeItem)?.i ?? -1;

            int count = (this.IsVertical ? this.deckGrid.RowDefinitions.Count : this.deckGrid.ColumnDefinitions.Count);

            for (int i = 0, slot = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    bool isActive = slot == activeIndex;

                    GridLength length = slot < this.ContentSizes.Count
                        ? this.ContentSizes[slot]
                        : this.MinContentSize;

                    if (this.IsVertical)
                    {
                        RowDefinition row = this.deckGrid.RowDefinitions[i];

                        if (isActive)
                        {
                            row.MinHeight = this.MinContentSize.Value;
                            row.Height = length;
                        }
                        else
                        {
                            row.MinHeight = 0;
                            row.Height = new GridLength(0);
                        }
                    }
                    else
                    {
                        ColumnDefinition col = this.deckGrid.ColumnDefinitions[i];

                        if (isActive)
                        {
                            col.MinWidth = this.MinContentSize.Value;
                            col.Width = length;
                        }
                        else
                        {
                            col.MinWidth = 0;
                            col.Width = new GridLength(0);
                        }
                    }

                    slot++;
                }
                else
                {
                    if (this.IsVertical)
                        this.deckGrid.RowDefinitions[i].Height = new GridLength(0);
                    else
                        this.deckGrid.ColumnDefinitions[i].Width = new GridLength(0);
                }
            }

            foreach (Control child in this.deckGrid.Children.OfType<Control>())
            {
                if (child is GridSplitter)
                {
                    child.IsVisible = false;

                    continue;
                }

                int slot = this.Items.IndexOf(((IContentItem)child.DataContext!));

                child.IsVisible = slot == activeIndex;
            }
        }

        private void RebuildGrid()
        {
            this.deckGrid.Children.Clear();
            this.deckGrid.RowDefinitions.Clear();
            this.deckGrid.ColumnDefinitions.Clear();

            int slotIndex = 0;

            for (int i = 0; i < this.Items.Count; i++)
            {
                if (this.Items[i] is not IContentItem switchable || switchable.Content == null)
                    continue;

                if (i > 0)
                {
                    if (this.IsVertical)
                        this.deckGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    else
                        this.deckGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                    int splitterIndex = this.IsVertical
                        ? this.deckGrid.RowDefinitions.Count - 1
                        : this.deckGrid.ColumnDefinitions.Count - 1;

                    GridSplitter splitter = this.CreateGridSplitter(splitterIndex);

                    this.deckGrid.Children.Add(splitter);
                }

                GridLength length = this.GetLengthOrMin(slotIndex);

                if (this.IsVertical)
                {
                    this.deckGrid.RowDefinitions.Add(new RowDefinition(length));
                    int rowIndex = this.deckGrid.RowDefinitions.Count - 1;
                    Grid.SetRow(switchable.Content, rowIndex);
                }
                else
                {
                    this.deckGrid.ColumnDefinitions.Add(new ColumnDefinition(length));
                    int colIndex = this.deckGrid.ColumnDefinitions.Count - 1;
                    Grid.SetColumn(switchable.Content, colIndex);
                }

                this.deckGrid.Children.Add(switchable.Content);

                slotIndex++;
            }

            this.UpdateGridVisibility();
        }
        private void ClearDeckGrid()
        {
            this.deckGrid.Children.Clear();
            this.deckGrid.RowDefinitions.Clear();
            this.deckGrid.ColumnDefinitions.Clear();
            this.ClearActiveItem();
        }

        #endregion
    }
}