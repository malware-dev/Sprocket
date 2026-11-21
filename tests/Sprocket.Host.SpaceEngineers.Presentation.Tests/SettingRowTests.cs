using Sprocket.Host.SpaceEngineers.Features.Installations;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A setting stands among the slots of the installation panel as a slot does: its mark and name, the value this installation
/// gives it where a slot shows its blocks, the bin and the Unused note, and its grip.
/// </summary>
public class SettingRowTests
{
    /// <summary>A setting shows the setting mark, its name, and the default said as such where this installation gives it no value.</summary>
    [Fact]
    public void ASettingShowsItsMarkItsNameAndItsDefault()
    {
        var screen = Panel(Real("Speed", 1.5));
        var row = Row(screen, "Speed");

        Assert.Equal(Symbols.Setting, row.Mark);
        Assert.Equal("Speed", row.NameButton.AutomationId);
        Assert.Equal("1.5", row.ValueChip.Text);
        Assert.False(row.ValueChip.IsEmpty);
    }

    /// <summary>A setting with no default shows the empty part a required slot shows.</summary>
    [Fact]
    public void ARequiredSettingNothingGivesShowsTheEmptyPart()
    {
        var screen = Panel(Required("Speed"));
        Use(screen, "Speed");
        var row = Row(screen, "Speed");

        Assert.Equal(Symbols.Setting, row.Mark);
        Assert.True(row.ValueChip.IsEmpty);
    }

    /// <summary>Pressing the value gives this installation its own value, shown plainly; one step back brings the default back.</summary>
    [Fact]
    public void PressingTheValueGivesThisInstallationItsOwn()
    {
        var screen = Panel(Real("Speed", 1.5));
        Row(screen, "Speed").Fills!.Execute();
        screen.Run(2);

        var asking = Page(screen).Asking;
        Assert.NotNull(asking);
        Assert.Equal("1.5", asking.Form.Fields[0].Text);
        asking.Form.Fields[0].Text = "2.5";
        asking.Done();
        screen.Run(2);

        Assert.True(Shown(screen).AnswerTo("Speed", out var own));
        Assert.Equal(2.5, own.Number);
        Assert.Equal("2.5", Row(screen, "Speed").ValueChip.Text);

        screen.Editor.Undo(System(screen), System(screen));
        screen.Run(2);
        Assert.False(Shown(screen).AnswerTo("Speed", out _));
        Assert.Equal("1.5", Row(screen, "Speed").ValueChip.Text);
    }

    /// <summary>"Use the default" takes the installation's own value away, and is offered only where the setting has a default.</summary>
    [Fact]
    public void UseTheDefaultClearsTheInstallationsValue()
    {
        var screen = Panel(Real("Speed", 1.5), Required("Load"));
        Shown(screen).Answer("Speed", Literal.Of(4.0));
        screen.Editor.Invalidate();
        screen.Run(2);
        Assert.Equal("4", Row(screen, "Speed").ValueChip.Text);

        Row(screen, "Speed").Fills!.Execute();
        screen.Run(2);
        var asking = Page(screen).Asking!;
        Assert.True(asking.UsesDefault.IsVisible);
        Assert.Equal(screen.Editor.Texts.SettingUseDefault, asking.UsesDefault.Text);
        asking.UsesDefault.Command!.Execute();
        screen.Run(2);

        Assert.False(Shown(screen).AnswerTo("Speed", out _));
        Assert.Equal("1.5", Row(screen, "Speed").ValueChip.Text);

        Row(screen, "Load").Fills!.Execute();
        screen.Run(2);
        Assert.False(Page(screen).Asking!.UsesDefault.IsVisible);
    }

    /// <summary>The installation's own panel gives the value too, where the slots are filled.</summary>
    [Fact]
    public void TheInstallationsOwnPanelGivesTheValueToo()
    {
        var screen = Panel(Real("Speed", 1.5));
        Use(screen, "Speed");
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.RootRows()[0].Opens!.Execute();
        screen.Run(2);

        var row = Within<PartRow>(screen).Single(part => part.Name == "Speed");
        Assert.Equal("1.5", row.ValueChip.Text);
        Assert.Null(row.NameButton.Command);
        row.Fills!.Execute();
        screen.Run(2);
        var asking = Assert.IsType<BlueprintPage>(screen.Editor.Current).Asking;
        Assert.NotNull(asking);
        asking.Form.Fields[0].Text = "3";
        asking.Done();
        screen.Run(2);

        Assert.Equal("3", Within<PartRow>(screen).Single(part => part.Name == "Speed").ValueChip.Text);
    }

    /// <summary>Pressing the name opens its name, what it holds and its default; renaming it keeps the states that start at it.</summary>
    [Fact]
    public void RenamingASettingKeepsEveryUseOfIt()
    {
        var screen = Panel(Real("Speed", 1.5));
        var system = System(screen);
        system.States.Add(new State("pace", "", Literal.Setting("Speed")));
        screen.Editor.Invalidate();
        screen.Run(2);

        Row(screen, "Speed").Opens!.Execute();
        screen.Run(2);
        var asking = Page(screen).Asking!;
        Assert.Equal(3, asking.Form.Count);
        Assert.Equal("Speed", asking.Form.Fields[0].Text);
        Assert.Equal("1.5", asking.Form.Fields[2].Text);

        asking.Form.Fields[0].Text = "Pace";
        asking.Done();
        screen.Run(2);

        Assert.NotNull(system.FindPart("Pace"));
        Assert.Null(system.FindPart("Speed"));
        Assert.Equal("Pace", system.States[0].Start.Text);

        screen.Editor.Undo(system, system);
        screen.Run(2);
        Assert.NotNull(system.FindPart("Speed"));
        Assert.Equal("Speed", system.States[0].Start.Text);
    }

    /// <summary>A setting's panel takes what it is for, as a slot's does, written as the panel is done, as one step back.</summary>
    [Fact]
    public void ASettingIsDescribedInItsPanel()
    {
        var screen = Panel(Real("Speed", 1.5));
        var editor = screen.Editor;
        Row(screen, "Speed").Opens!.Execute();
        screen.Run(2);
        var asking = Page(screen).Asking!;

        editor.Input.Focus(asking.About().Box);
        screen.Do("type", "How fast the door moves.").Run(1);
        asking.Done();
        screen.Run(2);

        var part = System(screen).FindPart("Speed")!;
        Assert.Equal("How fast the door moves.", part.Annotation);
        Assert.Equal(1.5, part.Default.Number);
        editor.Undo(System(screen), System(screen));
        screen.Run(2);
        Assert.Equal("", part.Annotation);
    }

    /// <summary>A setting with no default opens on an empty default, not on a value of another kind.</summary>
    [Fact]
    public void ASettingWithNoDefaultOpensOnAnEmptyDefault()
    {
        var screen = Panel(Required("Speed"), new Part("Note") { Setting = "text" });
        foreach (var name in new[] { "Speed", "Note" })
        {
            Row(screen, name).Opens!.Execute();
            screen.Run(2);
            var asking = Page(screen).Asking!;
            Assert.Equal("", asking.Form.Fields[2].Text);
            asking.Finish(true);
            screen.Run(2);
        }
    }

    /// <summary>Enter in the panel's name moves on to the next value and leaves the panel open; Enter in its last value makes the setting.</summary>
    [Fact]
    public void EnterMovesOnUntilTheLastValue()
    {
        var screen = Panel();
        var made = Add(screen);
        screen.Editor.Input.Focus(made.Form.Fields[0].Keyboard);

        screen.Do("enter").Run(2);
        Assert.True(made.IsOpen);
        Assert.NotSame(made.Form.Fields[0].Keyboard, screen.Editor.Input.Focused);

        for (var i = 0; i < 5 && made.IsOpen; i++)
            screen.Do("enter").Run(2);

        Assert.False(made.IsOpen);
        Assert.Single(System(screen).Parts, one => one.IsSetting);
    }

    /// <summary>A setting made from the panel takes what it is for from the panel too.</summary>
    [Fact]
    public void ANewSettingIsDescribedAsItIsMade()
    {
        var screen = Panel();
        var made = Add(screen);
        screen.Editor.Input.Focus(made.About().Box);
        screen.Do("type", "What the screen says.").Run(1);
        made.Done();
        screen.Run(2);

        Assert.Equal("What the screen says.", System(screen).Parts.Single(one => one.IsSetting).Annotation);
    }

    /// <summary>A setting's name another slot or setting has, in any case, is refused: the panel says why and stays open.</summary>
    [Fact]
    public void ANameAnotherHasIsRefused()
    {
        var screen = Panel(Real("Speed", 1.5), Real("Pace", 2));
        Row(screen, "Speed").Opens!.Execute();
        screen.Run(2);
        var asking = Page(screen).Asking!;
        asking.Form.Fields[0].Text = "PACE";
        asking.Done();
        screen.Run(2);

        Assert.True(asking.IsOpen);
        Assert.Equal(screen.Editor.Texts.RenameTaken, asking.Why);
        Assert.NotNull(System(screen).FindPart("Speed"));
    }

    /// <summary>A percentage is set on a slider, which always stands at a value, so a new one starts with a default and is never left without one.</summary>
    [Fact]
    public void APercentageAlwaysHasADefault()
    {
        var screen = Panel();
        var made = Add(screen);
        Choose(screen, made, screen.Editor.Texts.SettingKindPercent);
        Assert.IsType<SliderField>(made.Form.Fields[2]);
        Assert.Equal(0.0, double.Parse(made.Form.Fields[2].Text, global::System.Globalization.CultureInfo.InvariantCulture));
        made.Done();
        screen.Run(2);

        var part = System(screen).Parts.Single(one => one.IsSetting);
        Assert.True(part.HasDefault);
        Assert.Equal(0.0, part.Default.Number);
    }

    /// <summary>A percentage made from a value's mark with nothing given there opens on the least it may be, as one made from the panel does.</summary>
    [Fact]
    public void APercentageMadeFromAnEmptyValueOpensOnItsLeast()
    {
        var screen = Panel();
        var marked = SettingPanel.New(screen.Editor, System(screen), SettingEditor.Percent.Name, default(Literal), () => Page(screen).View, (_, _) => { }, () => { });
        screen.Run(2);

        Assert.Equal(0.0, double.Parse(marked.Form.Fields[2].Text, global::System.Globalization.CultureInfo.InvariantCulture));
        marked.Finish(true);
    }

    /// <summary>Emptying the default makes the setting one every installation must give, and one step back gives the default back.</summary>
    [Fact]
    public void EmptyingTheDefaultMakesTheSettingRequired()
    {
        var screen = Panel(Real("Speed", 1.5));
        Use(screen, "Speed");
        Row(screen, "Speed").Opens!.Execute();
        screen.Run(2);
        var asking = Page(screen).Asking!;
        asking.Form.Fields[2].Text = "";
        asking.Done();
        screen.Run(2);

        var part = System(screen).FindPart("Speed")!;
        Assert.False(part.HasDefault);
        Assert.False(part.Optional);
        Assert.True(Row(screen, "Speed").ValueChip.IsEmpty);

        screen.Editor.Undo(System(screen), System(screen));
        screen.Run(2);
        Assert.True(part.HasDefault);
        Assert.Equal(1.5, part.Default.Number);
    }

    /// <summary>A setting nothing uses says Unused and has a bin that removes it; one a state starts at has no bin to press.</summary>
    [Fact]
    public void TheBinAndUnusedActAsOnASlot()
    {
        var screen = Panel(Real("Speed", 1.5), Real("Pace", 1.0));
        System(screen).States.Add(new State("rate", "", Literal.Setting("Pace")));
        screen.Editor.Invalidate();
        screen.Run(2);

        var unused = Row(screen, "Speed");
        Assert.Equal(screen.Editor.Texts.SlotUnused, unused.Problem);
        Assert.NotNull(unused.Removes);
        var used = Row(screen, "Pace");
        Assert.Null(used.Problem);
        Assert.Null(used.Removes);

        unused.Removes!.Execute();
        screen.Run(2);
        Assert.Null(System(screen).FindPart("Speed"));
    }

    /// <summary>A setting is carried by its grip among the slots and into a category, as a slot is.</summary>
    [Fact]
    public void ASettingIsDraggedAmongTheSlotsAndIntoACategory()
    {
        var screen = Panel(Real("Speed", 1.5));
        Assert.Equal("Speed", System(screen).Parts.Last().Id);

        Carry(screen, Row(screen, "Speed"), Page(screen).Slots.OfType<PartRow>().First(), false);
        Assert.Equal("Speed", System(screen).Parts[0].Id);

        Page(screen).View.Within().OfType<Button>().Single(button => button.Text.EndsWith(screen.Editor.Texts.CategoryAdd)).Command!.Execute();
        screen.Run(5);
        var end = Page(screen).Slots.OfType<CategoryEndRow>().Single();
        Carry(screen, Row(screen, "Speed"), end, false);
        Assert.Equal("Category 1", System(screen).FindPart("Speed")!.Category);
    }

    /// <summary>The "+ slot" menu offers a setting, under the slots and at the end of a category.</summary>
    [Fact]
    public void ThePlusSlotMenuOffersASetting()
    {
        var screen = Panel();
        Assert.Contains(screen.Editor.Texts.SlotAddSetting, Page(screen).View.Within().OfType<PlusMenu>().SelectMany(menu => menu.Choices).Select(choice => choice.Text));

        Page(screen).View.Within().OfType<Button>().Single(button => button.Text.EndsWith(screen.Editor.Texts.CategoryAdd)).Command!.Execute();
        screen.Run(5);
        var end = Page(screen).Slots.OfType<CategoryEndRow>().Single();
        Assert.Contains(screen.Editor.Texts.SlotAddSetting, end.Adds.Choices.Select(choice => choice.Text));
    }

    /// <summary>Each kind of setting is made from the panel, its default following the kind chosen; one step back takes it away.</summary>
    [Theory]
    [InlineData(0, "flag", "off")]
    [InlineData(1, "integer", "3")]
    [InlineData(2, "real", "2.5")]
    [InlineData(3, "text", "hello")]
    [InlineData(4, "duration", "4")]
    public void EachKindOfSettingIsMadeFromThePanel(int kind, string type, string typed)
    {
        var screen = Panel();
        var asking = Add(screen);
        Choose(screen, asking, SettingPanel.KindWords(screen.Editor.Texts)[kind]);
        asking = Page(screen).Asking!;
        asking.Form.Fields[0].Text = "Made";
        asking.Form.Fields[2].Text = typed;
        asking.Done();
        screen.Run(2);

        var part = System(screen).FindPart("Made")!;
        Assert.Equal(type, part.Setting);
        Assert.True(part.HasDefault);
        Assert.Equal("Made", System(screen).Parts.Last().Id);

        screen.Editor.Undo(System(screen), System(screen));
        screen.Run(2);
        Assert.Null(System(screen).FindPart("Made"));
    }

    /// <summary>
    /// A percentage is made from the panel as a number edited as a percentage: its default set with a slider from 0 to 100, and
    /// its row saying it with its unit.
    /// </summary>
    [Fact]
    public void APercentageIsMadeFromThePanelAndSaidWithItsUnit()
    {
        var screen = Panel();
        var asking = Add(screen);
        Choose(screen, asking, screen.Editor.Texts.SettingKindPercent);
        asking = Page(screen).Asking!;
        Assert.IsType<SliderField>(asking.Form.Fields[2]);
        asking.Form.Fields[0].Text = "Opening";
        asking.Form.Fields[2].Text = "50";
        asking.Done();
        screen.Run(2);

        var part = System(screen).FindPart("Opening")!;
        Assert.Equal("real", part.Setting);
        Assert.Equal(SettingEditor.Percent.Name, part.Editor);
        Assert.Equal("50 %", Row(screen, "Opening").ValueChip.Text);
    }

    /// <summary>A percentage's own value for this installation is given with a slider, and its row says it with its unit.</summary>
    [Fact]
    public void APercentagesOwnValueIsGivenWithASlider()
    {
        var opening = Real("Opening", 50);
        opening.Editor = SettingEditor.Percent.Name;
        var screen = Panel(opening);
        Assert.Equal("50 %", Row(screen, "Opening").ValueChip.Text);

        Row(screen, "Opening").Fills!.Execute();
        screen.Run(2);
        var asking = Page(screen).Asking!;
        Assert.IsType<SliderField>(asking.Form.Fields[0]);
        asking.Form.Fields[0].Text = "75";
        asking.Done();
        screen.Run(2);

        Assert.True(Shown(screen).AnswerTo("Opening", out var own));
        Assert.Equal(75, own.Number);
        Assert.Equal("75 %", Row(screen, "Opening").ValueChip.Text);
    }

    /// <summary>A setting made for a value that is a percentage, such as how far a door is open, is a percentage too.</summary>
    [Fact]
    public void ASettingMadeForAPercentageIsAPercentage()
    {
        var screen = Panel();
        var door = screen.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door").Kind;
        var open = door.Actions.First(action => action.Id == "how_far_open");
        var compared = ComparisonQuestion.Fields(open, door.Events)[1];
        var shaped = SettingEditor.Of(compared);
        Assert.Same(SettingEditor.Percent, shaped);

        Part? made = null;
        var asking = SettingPanel.New(screen.Editor, System(screen), shaped!.Name, Literal.Of(25.0), () => Page(screen).View!, (part, _) => made = part, () => { });
        screen.Run(2);
        Assert.IsType<SliderField>(asking.Form.Fields[2]);
        asking.Form.Fields[0].Text = "Opening";
        asking.Done();
        screen.Run(2);

        Assert.NotNull(made);
        Assert.Equal("real", made!.Setting);
        Assert.Equal(SettingEditor.Percent.Name, made.Editor);
    }

    /// <summary>Leaving the default empty makes a new setting required.</summary>
    [Fact]
    public void AnEmptyDefaultMakesANewSettingRequired()
    {
        var screen = Panel();
        var asking = Add(screen);
        Choose(screen, asking, SettingPanel.KindWords(screen.Editor.Texts)[3]);
        asking = Page(screen).Asking!;
        asking.Form.Fields[0].Text = "Made";
        asking.Done();
        screen.Run(2);

        var part = System(screen).FindPart("Made")!;
        Assert.Equal("text", part.Setting);
        Assert.False(part.HasDefault);
        Assert.False(part.Optional);
    }

    /// <summary>One of the logic system's own state types is chosen from what the setting holds, and its default is among that type's choices.</summary>
    [Fact]
    public void ADeclaredTypeIsChosenFromHolds()
    {
        var screen = Panel();
        WithSide(screen);

        var asking = Add(screen);
        Choose(screen, asking, "side");
        asking = Page(screen).Asking!;
        asking.Form.Fields[0].Text = "Door side";
        asking.Form.Fields[2].Text = "outside";
        asking.Done();
        screen.Run(2);

        var part = System(screen).FindPart("Door side")!;
        Assert.Equal("side", part.Setting);
        Assert.Equal("outside", part.Default.Said);
    }

    /// <summary>"New state type…" makes a type for the setting and chooses it; the type and the setting come as one change.</summary>
    [Fact]
    public void ANewStateTypeIsMadeFromHolds()
    {
        var screen = Panel();
        var asking = Add(screen);
        Choose(screen, asking, screen.Editor.Texts.SettingNewType);
        asking = Page(screen).Asking!;
        var made = asking.Form.Fields[1].Text;
        Assert.NotEmpty(made);
        Assert.NotEqual(screen.Editor.Texts.SettingNewType, made);
        asking.Form.Fields[0].Text = "Mode";
        asking.Done();
        screen.Run(2);

        Assert.Equal(made, System(screen).FindPart("Mode")!.Setting);
        Assert.NotNull(System(screen).FindType(made));

        screen.Editor.Undo(System(screen), System(screen));
        screen.Run(2);
        Assert.Null(System(screen).FindPart("Mode"));
        Assert.Null(System(screen).FindType(made));
    }

    /// <summary>What a setting holds cannot be chosen where it is already decided: from a value's mark, and on a setting that exists.</summary>
    [Fact]
    public void HoldsCannotBeChangedWhereItIsDecided()
    {
        var screen = Panel(Real("Speed", 1.5));
        Row(screen, "Speed").Opens!.Execute();
        screen.Run(2);
        var editing = Page(screen).Asking!;
        Assert.False(((Element)editing.Form.Fields[1]).IsEnabled);
        Assert.Equal(screen.Editor.Texts.SettingKindReal, editing.Form.Fields[1].Text);
        editing.Finish(true);

        var marked = SettingPanel.New(screen.Editor, System(screen), "real", default(Literal), () => Page(screen).View, (_, _) => { }, () => { });
        screen.Run(2);
        Assert.False(((Element)marked.Form.Fields[1]).IsEnabled);
        marked.Finish(true);

        var free = Add(screen);
        Assert.True(((Element)free.Form.Fields[1]).IsEnabled);
    }

    /// <summary>The panel keeps its size however its default is asked, whatever the setting holds.</summary>
    [Fact]
    public void ThePanelKeepsItsSizeWhenHoldsChanges()
    {
        var screen = Panel();
        WithSide(screen);

        var asking = Add(screen);
        var sizes = new List<Vector2>();
        foreach (var word in SettingPanel.KindWords(screen.Editor.Texts).Concat(new[] { "side" }))
        {
            Choose(screen, asking, word);
            asking = Page(screen).Asking!;
            sizes.Add(asking.Window.Bounds.Size);
        }

        Assert.All(sizes, size => Assert.Equal(sizes[0].Y, size.Y, 1));
        Assert.All(sizes, size => Assert.Equal(sizes[0].X, size.X, 1));
    }

    /// <summary>Opened on a setting that exists, or from a value's mark, the panel stands in the room it has when a setting is made from the panel.</summary>
    [Fact]
    public void ThePanelKeepsItsSizeWhereHoldsIsDecided()
    {
        var screen = Panel(Real("Speed", 1.5));
        var made = Add(screen);
        var room = made.Window.Bounds.Size;
        made.Finish(true);

        Row(screen, "Speed").Opens!.Execute();
        screen.Run(2);
        var editing = Page(screen).Asking!;
        Assert.Equal(room.X, editing.Window.Bounds.Size.X, 1);
        Assert.Equal(room.Y, editing.Window.Bounds.Size.Y, 1);
        editing.Finish(true);

        var marked = SettingPanel.New(screen.Editor, System(screen), "real", default(Literal), () => Row(screen, "Speed").ValueChip, (_, _) => { }, () => { });
        screen.Run(2);
        Assert.Equal(room.X, marked.Window.Bounds.Size.X, 1);
        Assert.Equal(room.Y, marked.Window.Bounds.Size.Y, 1);
    }

    static void WithSide(Screen screen)
    {
        var type = new DeclaredType("side");
        type.Choices.Add("inside");
        type.Choices.Add("outside");
        System(screen).Types.Add(type);
        screen.Editor.Invalidate();
        screen.Run(2);
    }

    static Part Real(string name, double value) => new Part(name) { Setting = "real", Optional = true, HasDefault = true, Default = Literal.Of(value) };

    static Part Required(string name) => new Part(name) { Setting = "real" };

    /// <summary>The installation panel of a logic system with the slots of an airlock, and <paramref name="settings" /> among them.</summary>
    static Screen Panel(params Part[] settings)
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Outer Door");
        editor.PressChoice("close");
        editor.PressPrimary();
        screen.Run(2);

        editor.GoToRoot();
        screen.Run(5);
        editor.Program.Systems[0].Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        foreach (var setting in settings)
            editor.MakeSetting(editor.Program.Systems[0], setting);

        editor.PressTab(editor.Texts.SystemInstallationPanel);
        return screen.Run(5);
    }

    /// <summary>Has a state of the logic system start at the setting <paramref name="name" />, so the setting is used.</summary>
    static void Use(Screen screen, string name)
    {
        System(screen).States.Add(new State("start of " + name, "", Literal.Setting(name)));
        screen.Editor.Invalidate();
        screen.Run(2);
    }

    static AutomationSystem System(Screen screen) => screen.Editor.Program.Systems[0];

    static SystemInstance Shown(Screen screen) => screen.Editor.StandIn(System(screen))!;

    static LogicSystemPage Page(Screen screen) => (LogicSystemPage)screen.Editor.Current!;

    static PartRow Row(Screen screen, string name) => Page(screen).Slots.OfType<PartRow>().Single(row => row.Name == name);

    static IEnumerable<T> Within<T>(Screen screen) where T : Element => screen.Editor.Current!.View!.Within().OfType<T>();

    /// <summary>Presses "a setting" in the menu under the slots, and answers the panel it opens.</summary>
    static ValuesQuestion Add(Screen screen)
    {
        var add = Page(screen).View!.Within().OfType<PlusMenu>().SelectMany(menu => menu.Choices).First(choice => choice.Text == screen.Editor.Texts.SlotAddSetting);
        add.Adds!.Execute();
        screen.Run(2);
        return Page(screen).Asking!;
    }

    /// <summary>Chooses the word <paramref name="word" /> in the list of what the setting <paramref name="asking" /> makes holds.</summary>
    static void Choose(Screen screen, ValuesQuestion asking, string word)
    {
        var list = (DropDown)asking.Form.Fields[1];
        var item = list.Items.OfType<DropDownItem>().Single(one => one.Text == word);
        item.Command!.Execute();
        screen.Run(2);
    }

    /// <summary>Takes <paramref name="row" /> by its grip and lets it go over the top or bottom edge of <paramref name="onto" />.</summary>
    static void Carry(Screen screen, Element row, Element onto, bool below)
    {
        var grip = (Element)((IReorderable)row).DragHandle;
        var from = grip.ToScreen(grip.Bounds.Center);
        var edge = onto.ToScreen(new Vector2(onto.Bounds.X, below ? onto.Bounds.Bottom - 2f : onto.Bounds.Y + 2f));
        var down = row == onto ? 0f : edge.Y - row.ToScreen(row.Bounds.Center).Y + (below ? row.Bounds.Height / 2f : -row.Bounds.Height / 2f);
        var to = new Vector2(from.X, from.Y + down);

        screen.Scene.MoveTo(from);
        screen.Scene.Down(from);
        screen.Run(1);
        screen.Scene.Move(new Vector2((from.X + to.X) / 2f, (from.Y + to.Y) / 2f));
        screen.Run(1);
        screen.Scene.Move(to);
        screen.Run(1);
        screen.Scene.Up();
        screen.Run(5);
    }
}
