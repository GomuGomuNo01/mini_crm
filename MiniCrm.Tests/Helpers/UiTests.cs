using MiniCrm.Helpers;
using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Tests.Helpers;

// Règles de présentation partagées par toutes les vues (libellés, monogrammes,
// durée de vie des contrats, lecture du journal d'audit, pagination).
public class UiTests
{
    private static readonly DateTime Today = new(2026, 10, 12);

    private static Contract Contract(ContractStatus status, DateTime start, DateTime end) =>
        new() { Status = status, StartDate = start, EndDate = end };

    // ---------------------------------------------------------------- Libellés

    [Theory]
    [InlineData(ContractStatus.Active, "Actif", "success")]
    [InlineData(ContractStatus.Expired, "Expiré", "neutral")]
    [InlineData(ContractStatus.Cancelled, "Annulé", "danger")]
    [InlineData(ContractStatus.Draft, "Brouillon", "warning")]
    public void Of_ContractStatus_ReturnsFrenchLabelAndTone(ContractStatus status, string label, string tone)
    {
        var look = Ui.Of(status);

        Assert.Equal(label, look.Label);
        Assert.Equal(tone, look.Tone);
        Assert.StartsWith("bi-", look.Icon);
    }

    [Fact]
    public void ContractStatusOptions_UsesEnumNamesAsValues_AndMarksSelection()
    {
        var options = Ui.ContractStatusOptions(ContractStatus.Draft).ToList();

        Assert.Equal(4, options.Count);
        Assert.Equal("Draft", options.Single(o => o.Selected).Value);
        Assert.Contains(options, o => o.Value == "Active" && o.Text == "Actif");
    }

    // ------------------------------------------------------------ Monogrammes

    [Theory]
    [InlineData("Boulangerie Martin", "BM")]
    [InlineData("Cabinet Dupont & Associés", "CD")]
    [InlineData("TechNova", "TE")]
    [InlineData("admin@minicrm.dev", "AD")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    public void Initials_TakesFirstLettersOfFirstTwoWords(string? name, string expected)
    {
        Assert.Equal(expected, Ui.Initials(name));
    }

    [Fact]
    public void Hue_IsStableAndWithinPalette()
    {
        var first = Ui.Hue("Studio Pixel");

        Assert.Equal(first, Ui.Hue("Studio Pixel"));
        Assert.Equal(first, Ui.Hue("studio pixel"));
        Assert.InRange(first, 0, 5);
        Assert.Equal(0, Ui.Hue(null));
    }

    [Fact]
    public void Money_DropsDecimalsOnlyWhenRound()
    {
        Assert.Equal("12 000 €", Ui.Money(12000m));
        Assert.Equal("2 400,50 €", Ui.Money(2400.5m));
    }

    // ------------------------------------------------ Durée de vie d'un contrat

    [Fact]
    public void Lifespan_ActiveContract_ReportsProgressAndRemainingMonths()
    {
        var c = Contract(ContractStatus.Active, Today.AddMonths(-3), Today.AddMonths(9));

        var life = Ui.LifespanOf(c, Today);

        Assert.Equal("success", life.Tone);
        Assert.InRange(life.Percent, 24, 26);
        Assert.Equal("9 mois restants", life.Caption);
        Assert.True(life.ShowNow);
        Assert.False(life.IsOverdue);
    }

    [Fact]
    public void Lifespan_ActiveContractEndingWithin30Days_IsAnAlert()
    {
        var c = Contract(ContractStatus.Active, Today.AddMonths(-11), Today.AddDays(12));

        var life = Ui.LifespanOf(c, Today);

        Assert.Equal("alert", life.Tone);
        Assert.Equal("Expire dans 12 jours", life.Caption);
    }

    [Fact]
    public void Lifespan_ActiveContractEndingToday_SaysSo()
    {
        var c = Contract(ContractStatus.Active, Today.AddYears(-1), Today);

        Assert.Equal("Expire aujourd'hui", Ui.LifespanOf(c, Today).Caption);
    }

    [Fact]
    public void Lifespan_ActiveContractPastItsEndDate_IsOverdue()
    {
        var c = Contract(ContractStatus.Active, Today.AddYears(-1), Today.AddDays(-88));

        var life = Ui.LifespanOf(c, Today);

        Assert.True(life.IsOverdue);
        Assert.Equal("danger", life.Tone);
        Assert.Equal(100, life.Percent);
        Assert.Equal("Échéance dépassée de 88 jours", life.Caption);
    }

    [Fact]
    public void Lifespan_ActiveContractNotStartedYet_CountsDownToStart()
    {
        var c = Contract(ContractStatus.Active, Today.AddDays(1), Today.AddYears(1));

        var life = Ui.LifespanOf(c, Today);

        Assert.Equal(0, life.Percent);
        Assert.Equal("Débute dans 1 jour", life.Caption);
    }

    [Theory]
    [InlineData(ContractStatus.Draft, "warning", "En attente de signature")]
    [InlineData(ContractStatus.Cancelled, "neutral", "Annulé")]
    public void Lifespan_InactiveStatuses_HaveNoTodayMarker(ContractStatus status, string tone, string caption)
    {
        var c = Contract(status, Today.AddMonths(-1), Today.AddMonths(5));

        var life = Ui.LifespanOf(c, Today);

        Assert.Equal(tone, life.Tone);
        Assert.Equal(caption, life.Caption);
        Assert.False(life.ShowNow);
    }

    // ------------------------------------------------------- Journal d'audit

    [Fact]
    public void ParseDetails_SplitsEachChangeIntoFieldBeforeAndAfter()
    {
        var changes = Ui.ParseDetails("Nom : « client_test » → « client_test_mod »\nSecteur : « (vide) » → « Bâtiment »");

        Assert.Equal(2, changes.Count);
        Assert.True(changes[0].IsDiff);
        Assert.Equal("Nom", changes[0].Field);
        Assert.Equal("client_test", changes[0].Old);
        Assert.Equal("client_test_mod", changes[0].New);
        Assert.Equal("(vide)", changes[1].Old);
    }

    [Fact]
    public void ParseDetails_TranslatesStatusValues()
    {
        var change = Ui.ParseDetails("Statut : « Prospect » → « Active »").Single();

        Assert.Equal("Prospect", change.Old);
        Assert.Equal("Actif", change.New);
    }

    [Fact]
    public void ParseDetails_KeepsUnstructuredLinesAsRawText()
    {
        var change = Ui.ParseDetails("Rôle : Admin").Single();

        Assert.False(change.IsDiff);
        Assert.Equal("Rôle : Admin", change.Raw);
        Assert.Empty(Ui.ParseDetails(null));
    }

    [Fact]
    public void DayLabel_UsesRelativeWordsForTodayAndYesterday()
    {
        Assert.Equal("Aujourd'hui", Ui.DayLabel(Today.AddHours(15), Today));
        Assert.Equal("Hier", Ui.DayLabel(Today.AddDays(-1), Today));
        Assert.Equal("Jeudi 2 juillet", Ui.DayLabel(new DateTime(2026, 7, 2), Today));
        Assert.Equal("Mardi 3 juin 2025", Ui.DayLabel(new DateTime(2025, 6, 3), Today));
    }

    // ------------------------------------------------------------- Pagination

    [Fact]
    public void PagerWindow_ShowsEdgesAroundCurrentPageWithGaps()
    {
        var pager = new PagerModel(6, 12, p => $"?page={p}");

        Assert.Equal(new int?[] { 1, null, 5, 6, 7, null, 12 }, pager.Window().ToArray());
    }

    [Fact]
    public void PagerWindow_HasNoGapsWhenPagesAreContiguous()
    {
        var pager = new PagerModel(2, 3, p => $"?page={p}");

        Assert.Equal(new int?[] { 1, 2, 3 }, pager.Window().ToArray());
    }
}
