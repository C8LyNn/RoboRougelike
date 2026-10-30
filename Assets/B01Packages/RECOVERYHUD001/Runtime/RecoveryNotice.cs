using System;
using System.Globalization;

namespace B01.RecoveryHud
{
    // DISPLAY CONTRACT ONLY. All proof flags and numbers must come from a trusted
    // live host read inside its already-held canonical world/Save authority.
    // This code never grants combat, a victory, a loaner, a repair or a Save write.
    public enum RecoveryNoticeKind
    {
        Unknown = 0, SaveUncertain = 1, CoreDestroyed = 2,
        Ready = 3, LoanerAvailable = 4, LoanerSpent = 5,
        AwaitingNextVictory = 6, RepairRequired = 7
    }

    public sealed class RecoveryRead
    {
        public string RunId { get; set; } = "";
        public string SlotId { get; set; } = "";
        public long Epoch { get; set; }
        public bool VerifiedOwnerRead { get; set; }
        public bool SingleLiveOwner { get; set; }
        public bool PhysicalStateVerified { get; set; }
        public bool DurableCommitCertain { get; set; }
        public bool CoreAlive { get; set; }
        public bool NextChallengeEligible { get; set; }
        public bool NormalConstructionFunctional { get; set; }
        public bool LoanerAlreadySpent { get; set; }
        public bool LoanerOfferAuthorizedByHost { get; set; }
        public bool PendingForNextVictory { get; set; }
        public int PendingDestroyedParts { get; set; }
        public bool FreeRebuildAvailableFromTrustedWin { get; set; }
        public int? HostQuotedPaidScrap { get; set; }
        public int? OriginalPartPriceScrap { get; set; }
    }

    public sealed class RecoveryNotice
    {
        public RecoveryNoticeKind Kind { get; }
        public string Heading { get; }
        public string Explanation { get; }
        public string Detail { get; }
        public bool MayShowOwnerProvidedOffer { get; }

        public RecoveryNotice(RecoveryNoticeKind kind, string heading, string explanation, string detail, bool mayShowOffer)
        {
            Kind = kind; Heading = heading; Explanation = explanation;
            Detail = detail; MayShowOwnerProvidedOffer = mayShowOffer;
        }
    }

    public static class RecoveryNoticeBuilder
    {
        private static RecoveryNotice Deny(string reason) =>
            new RecoveryNotice(RecoveryNoticeKind.Unknown, "Stan robota niezweryfikowany",
                "Odśwież informacje o sesji przed wyborem działania.", reason, false);

        private static bool ValidToken(string value)
        {
            if (String.IsNullOrEmpty(value) || value.Length > 72) return false;
            foreach (char c in value)
                if (!(Char.IsLetterOrDigit(c) && c <= 127) && c != '-' && c != '_') return false;
            return true;
        }

        // This is only a display sanity check, NOT a quotation engine. Both
        // currently disputed owner-side rounding rules are permitted (floor and
        // half-up); neither is selected by this UI. The approved strict discount
        // excludes a 1-Scrap purchase -> 1-Scrap rebuild. Checked 64-bit
        // intermediates avoid overflow at Int32.MaxValue.
        private static bool ValidDiscountQuote(int purchase, int quoted)
        {
            if (purchase <= 0 || quoted < 0 || quoted >= purchase) return false;
            long scaled = (long)purchase * 65L;
            long floor = scaled / 100L;
            long halfUp = (scaled + 50L) / 100L;
            return quoted == floor || quoted == halfUp;
        }
        // expectedEpoch is a HOST epoch read before rendering, not a cryptographic
        // trust proof. Neither caller flags nor this view replace the owner-side CAS.
        public static RecoveryNotice Build(RecoveryRead input, string expectedRunId, string expectedSlotId, long expectedEpoch)
        {
            if (input == null || !ValidToken(expectedRunId) || !ValidToken(expectedSlotId) ||
                !ValidToken(input.RunId) || !ValidToken(input.SlotId) ||
                !String.Equals(input.RunId, expectedRunId, StringComparison.Ordinal) ||
                !String.Equals(input.SlotId, expectedSlotId, StringComparison.Ordinal) ||
                expectedEpoch < 0 || input.Epoch != expectedEpoch)
                return Deny("Nieaktualny albo obcy identyfikator sesji.");

            if (!input.VerifiedOwnerRead || !input.SingleLiveOwner || !input.PhysicalStateVerified)
                return Deny("Brak potwierdzenia jednego właściciela i fizycznego stanu.");

            if (!input.DurableCommitCertain)
                return new RecoveryNotice(RecoveryNoticeKind.SaveUncertain,
                    "Zapis wymaga weryfikacji",
                    "Nie rozpoczynaj napraw ani kolejnej walki, dopóki właściciel sesji nie potwierdzi zapisu.",
                    "Status zapisu jest niejednoznaczny. Żadna operacja nie została wykonana przez HUD.", false);

            if (input.PendingDestroyedParts < 0 || input.PendingDestroyedParts > 500 ||
                (input.PendingForNextVictory && input.PendingDestroyedParts == 0) ||
                (!input.PendingForNextVictory && input.PendingDestroyedParts > 0) ||
                (input.OriginalPartPriceScrap.HasValue && input.OriginalPartPriceScrap.Value <= 0) ||
                (input.HostQuotedPaidScrap.HasValue &&
                    (!input.OriginalPartPriceScrap.HasValue ||
                     !ValidDiscountQuote(input.OriginalPartPriceScrap.Value, input.HostQuotedPaidScrap.Value))) ||
                (input.LoanerOfferAuthorizedByHost &&
                    (input.LoanerAlreadySpent || !input.CoreAlive || !input.NextChallengeEligible ||
                     input.NormalConstructionFunctional)))
                return Deny("Sprzeczne dane o częściach, koszcie albo robocie zastępczym.");

            string cost = "";
            if (input.HostQuotedPaidScrap.HasValue)
                cost = " Wycena właściciela: " +
                    input.HostQuotedPaidScrap.Value.ToString(CultureInfo.InvariantCulture) +
                    " Scrap; zatwierdzona zasada: 65% ceny nowej części (zaokrąglenie ustala ekonomia).";
            string pending = input.PendingForNextVictory
                ? " Oczekujące odbudowy: " + input.PendingDestroyedParts.ToString(CultureInfo.InvariantCulture) +
                  ". Części zniszczone w bieżącym zwycięstwie wracają dopiero po następnym rzeczywistym zwycięstwie."
                : "";
            string free = input.FreeRebuildAvailableFromTrustedWin
                ? " Darmowa odbudowa: dostępna według zweryfikowanego zwycięstwa (jedna na zwycięstwo)."
                : " Brak zweryfikowanej dostępnej darmowej odbudowy.";
            string detail = (pending + free + cost).Trim();

            if (!input.CoreAlive)
                return new RecoveryNotice(RecoveryNoticeKind.CoreDestroyed,
                    "Core zniszczony", "Robot zastępczy jest niedostępny, gdy Core nie działa.",
                    detail, false);

            if (input.NormalConstructionFunctional)
                return new RecoveryNotice(RecoveryNoticeKind.Ready,
                    "Konstrukcja sprawna",
                    "Można kontynuować zwykłą walkę według decyzji autorytatywnego systemu rozgrywki. Uszkodzone opcjonalne części same nie blokują walki.",
                    detail, false);

            if (!input.NextChallengeEligible)
                return new RecoveryNotice(RecoveryNoticeKind.RepairRequired,
                    "Brak dostępnego wyzwania",
                    "Konstrukcja jest niesprawna, a właściciel nie potwierdził kolejnego wyzwania. Nie oferuj robota zastępczego.",
                    detail, false);

            if (input.LoanerOfferAuthorizedByHost)
                return new RecoveryNotice(RecoveryNoticeKind.LoanerAvailable,
                    "Dostępny jednorazowy robot zastępczy",
                    "Słabszy robot zastępczy może zostać zaproponowany raz przez uprawniony system gry. Twoje zniszczone części pozostają w kolejce.",
                    detail, true);

            if (input.LoanerAlreadySpent)
                return new RecoveryNotice(RecoveryNoticeKind.LoanerSpent,
                    "Robot zastępczy wykorzystany",
                    "Konstrukcja nadal nie spełnia warunków zwykłej walki. Nie przyznawaj drugiego robota zastępczego.",
                    detail, false);

            return new RecoveryNotice(input.PendingForNextVictory
                    ? RecoveryNoticeKind.AwaitingNextVictory : RecoveryNoticeKind.RepairRequired,
                input.PendingForNextVictory ? "Odbudowa czeka na kolejne zwycięstwo" : "Konstrukcja niesprawna",
                input.PendingForNextVictory
                    ? "Oczekująca odbudowa nie jest natychmiastową naprawą. Sprawdź bezpieczną ścieżkę dalszej rozgrywki."
                    : "Tylko rzeczywiście niesprawna konstrukcja ma blokadę zwykłej walki.",
                detail, false);
        }
    }
}
