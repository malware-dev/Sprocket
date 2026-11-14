using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Text;
using Sprocket.Sim.Content;
using Sprocket.Sim.Items;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The game's own items, read from its definitions, in the lists a person picks an item from.</summary>
/// <remarks>Each needs the game installed, or the harness's cache of its content, and passes without looking where neither is here.</remarks>
public class GameItemsTests : IDisposable
{
    /// <summary>Puts the game's own list of items back behind the mod, for whatever runs next.</summary>
    public void Dispose()
    {
        ItemIds.Use(null);
    }

    /// <summary>The game's items, or null where neither the game nor the cache has them.</summary>
    static GameStock? Stock()
    {
        var stock = GameStock.FromGame();
        return stock.IsRead ? stock : null;
    }

    /// <summary>A when trigger on <paramref name="block" />, a refinery's or a cargo container's, opened on its amount, the construct stocked with <paramref name="stock" />.</summary>
    static Screen Amount(GameStock stock, string block)
    {
        var rig = Rig.Demo();
        rig.Refinery(40, "Refinery 1");
        rig.Cargo(41, "Cargo 1");
        rig.Stocked(stock);
        var screen = Screen.On(rig);
        screen.Editor.NewSystem("Stock");
        screen.Editor.PressStart(SentenceText.WhenWord);
        screen.Editor.PressBlock(block);
        screen.Run(2);
        screen.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == "amount" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        screen.Run(2);
        return screen;
    }

    /// <summary>The list of items a form asks for, which is searched.</summary>
    static ListField Items(Screen screen) => screen.Editor.Pick().Arguments!.Form.Fields.OfType<ListField>().Single(list => list.IsSearchable);

    /// <summary>The words <paramref name="list" /> offers, in order.</summary>
    static List<string> Words(ListField list) => list.Items.OfType<DropDownItem>().Select(item => item.Text).ToList();

    /// <summary>The words <paramref name="list" /> still offers.</summary>
    static List<string> Found(ListField list) => list.Items.OfType<DropDownItem>().Where(item => item.IsVisible).Select(item => item.Text).ToList();

    /// <summary>Opens <paramref name="list" />, searches it for <paramref name="search" /> and presses the item found, as a person does.</summary>
    static void Choose(Screen screen, ListField list, string search)
    {
        Open(screen, list);
        screen.Do("type", search).Run(1);
        Press(screen, list.Items.OfType<DropDownItem>().Single(item => item.IsVisible));
    }

    /// <summary>Opens <paramref name="list" /> by pressing it, as a person does.</summary>
    static void Open(Screen screen, ListField list)
    {
        Press(screen, list);
        Assert.True(list.IsOpen, "the list did not open");
    }

    /// <summary>Presses the middle of <paramref name="element" /> with the pointer.</summary>
    static void Press(Screen screen, Element element)
    {
        var at = element.ToScreen(new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f));
        screen.Do(Say(at.X), Say(at.Y), "tick", "2");
    }

    static string Say(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The names of the public items the game counts as <paramref name="said" /> says.</summary>
    static IEnumerable<string> Named(GameStock stock, Func<string, bool> said) =>
        stock.Items.All.Where(item => item.IsPublic && said(item.Said)).Select(item => item.Name);

    /// <summary>An item is named in the language the game's texts are read in, by the text its definition names.</summary>
    [Fact]
    public void AnItemIsNamedInTheGamesLanguage()
    {
        if (Stock() == null)
            return;

        var norwegian = ItemDefinitions.FromGame("no").Find("Ingot/Iron");
        var english = ItemDefinitions.FromGame().Find("Ingot/Iron");

        Assert.Equal("Iron Ingot", english!.Name);
        Assert.Equal(GameLanguage.In("no")!.Find("DisplayName_Item_IronIngot"), norwegian!.Name);
        Assert.NotEqual(english.Name, norwegian.Name);
    }

    /// <summary>A refinery takes ores and gives ingots, an assembler takes ingots and gives components, as their blueprint classes say.</summary>
    [Fact]
    public void ProductionBlocksTakeAndGiveWhatTheirClassesMake()
    {
        var stock = Stock();
        if (stock == null)
            return;

        // The game's refinery takes old scrap, which the game counts as an ingot, beside the ores.
        Assert.All(stock.Taken("Refinery", "LargeRefinery", 0), item => Assert.True(item.StartsWith("Ore/") || item == "Ingot/Scrap", item));
        Assert.All(stock.Taken("Refinery", "LargeRefinery", 1), item => Assert.StartsWith("Ingot/", item));
        Assert.Contains("Ore/Iron", stock.Taken("Refinery", "LargeRefinery", 0));
        Assert.Contains("Ingot/Iron", stock.Taken("Refinery", "LargeRefinery", 1));

        // An assembler also takes the tools its better tools are made from.
        Assert.Contains("Ingot/Iron", stock.Taken("Assembler", "LargeAssembler", 0));
        Assert.DoesNotContain(stock.Taken("Assembler", "LargeAssembler", 0), item => item.StartsWith("Ore/"));
        Assert.Contains("Component/SteelPlate", stock.Taken("Assembler", "LargeAssembler", 1));
        Assert.DoesNotContain("Component/SteelPlate", stock.Taken("Assembler", "LargeAssembler", 0));

        var cargo = stock.Taken("CargoContainer", "LargeBlockLargeContainer", 0);
        Assert.Equal(stock.Items.All.Select(item => item.Said), cargo);
    }

    /// <summary>A refinery's item list holds the ores it takes and the ingots it gives, and nothing else, and the trigger asks which of its inventories.</summary>
    [Fact]
    public void TheRefineryListsOresAndIngotsAndNothingElse()
    {
        var stock = Stock();
        if (stock == null)
            return;

        var screen = Amount(stock, "Refinery 1");
        var words = Words(Items(screen));

        Assert.Contains("Iron Ore", words);
        Assert.Contains("Iron Ingot", words);
        Assert.DoesNotContain("Steel Plate", words);
        var ores = Named(stock, said => said.StartsWith("Ore/", StringComparison.Ordinal) || said.StartsWith("Ingot/", StringComparison.Ordinal)).ToList();
        Assert.All(words, word => Assert.Contains(word, ores));

        var inventory = screen.Editor.Pick().Arguments!.Form.Fields.OfType<WordsField>().Single();
        Assert.True(inventory.IsVisible);
        Assert.True(screen.Shows("input"), screen.Dump);
        Assert.True(screen.Shows("output"), screen.Dump);
    }

    /// <summary>A cargo container's item list holds every item a player sees, by the names the game shows, and the trigger asks nothing of which inventory.</summary>
    [Fact]
    public void TheCargoContainerListsEveryItemByItsName()
    {
        var stock = Stock();
        if (stock == null)
            return;

        var screen = Amount(stock, "Cargo 1");
        var words = Words(Items(screen));

        Assert.Equal(Named(stock, _ => true), words);
        Assert.Contains("Iron Ingot", words);
        Assert.Contains("Steel Plate", words);
        Assert.DoesNotContain(words, word => word.StartsWith("DisplayName_", StringComparison.Ordinal));
        Assert.False(screen.Shows("inventory"), screen.Dump);
        Assert.False(screen.Shows("input"), screen.Dump);
    }

    /// <summary>The number an amount is compared with says kg for an ingot and pcs for a component, following the item picked from the list.</summary>
    [Fact]
    public void AUnitFollowsTheItemPicked()
    {
        var stock = Stock();
        if (stock == null)
            return;

        var screen = Amount(stock, "Cargo 1");
        var form = screen.Editor.Pick().Arguments!.Form;
        var number = Assert.IsType<NumberBox>(form.Fields.OfType<NumberField>().Single(field => field.IsVisible).Children[0]);

        Choose(screen, Items(screen), "Iron Ingot");
        Assert.Equal("Iron Ingot", Items(screen).Text);
        Assert.EndsWith("' kg'", number.FormatString);

        Choose(screen, Items(screen), "Steel Plate");
        Assert.Equal("Steel Plate", Items(screen).Text);
        Assert.EndsWith("' pcs'", number.FormatString);
    }

    /// <summary>A move of items out of a cargo container opens on every item, then each item the game has, and search finds one by its name.</summary>
    [Fact]
    public void TheMovesListHoldsTheGamesItemsAndSearchFindsThem()
    {
        var stock = Stock();
        if (stock == null)
            return;

        var screen = Moving(stock);
        var list = Items(screen);

        var words = Words(list);
        Assert.Equal(InventoryType.AllItems, words[0]);
        Assert.Equal(Named(stock, _ => true), words.Skip(1));

        Open(screen, list);
        screen.Do("type", "steel pl").Run(1);
        Assert.Equal(new[] { "Steel Plate" }, Found(list));
    }

    /// <summary>Enter in a move's searched item list chooses what the search found, and leaves the step being picked.</summary>
    [Fact]
    public void EnterInTheSearchedListChoosesWhatItFound()
    {
        var stock = Stock();
        if (stock == null)
            return;

        var screen = Moving(stock);
        var list = Items(screen);
        var page = screen.Editor.Current;
        Open(screen, list);
        screen.Do("type", "steel pl").Run(1);
        screen.Do("enter").Run(2);

        Assert.Equal("Steel Plate", list.Text);
        Assert.Same(page, screen.Editor.Current);
    }

    /// <summary>A move's line says kg after an ingot's amount and pcs after a component's, by the names the game shows them.</summary>
    [Theory]
    [InlineData("Iron Ingot", "move 100 kg Iron Ingot from")]
    [InlineData("Steel Plate", "move 100 pcs Steel Plate from")]
    public void TheMovesLineSaysTheItemsUnit(string item, string said)
    {
        var stock = Stock();
        if (stock == null)
            return;

        var screen = Moving(stock);
        Choose(screen, Items(screen), item);
        Assert.Equal(item, Items(screen).Text);
        screen.Editor.Pick().Arguments!.Form.Fields.OfType<NumberField>().Single(field => field.IsVisible).Text = "100";
        ((SlotsField)screen.Editor.Pick().Arguments!.Form.Fields[0]).Select();
        screen.Run(2);
        screen.Editor.PressBlock("Cargo 2");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(4);

        var line = string.Join(" ", System.Text.RegularExpressions.Regex.Matches(screen.Dump, "\"([^\"]*)\"")
            .Cast<System.Text.RegularExpressions.Match>().Select(match => match.Groups[1].Value));
        Assert.True(line.Contains(said), line);
    }

    /// <summary>A step moving items out of a cargo container, opened, with a second container to move them to.</summary>
    static Screen Moving(GameStock stock)
    {
        var rig = Rig.Demo();
        rig.Cargo(40, "Cargo 1");
        rig.Cargo(41, "Cargo 2");
        rig.Stocked(stock);
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Stock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Cargo 1");
        screen.Run(2);
        screen.Editor.PressChoice("move items");
        screen.Run(2);
        return screen;
    }
}
