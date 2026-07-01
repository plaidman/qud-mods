using System;
using XRL;

namespace Plaidman.SaltShuffleRevival {
    [HasOptionFlagUpdate(Prefix = "Plaidman_SaltShuffleRevival_")]
    public static class Options {
        [OptionFlag] public static bool EnableCardLongNames;
        [OptionFlag] public static bool EnableCardNameColors;
    }
}