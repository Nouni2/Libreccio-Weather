/*
 * This file is part of Libreccio Weather.
 *
 * Libreccio Weather is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, version 3 of the License.
 *
 * Libreccio Weather is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Libreccio Weather. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.Localization;

/// <summary>
/// Shows digits as the selected language writes them, such as Arabic-Indic digits in Arabic, as Android formats
/// numbers with the language's default CLDR digits. Numbers formatted by .NET carry Latin digits, which WPF draws in the
/// language's own digits through number substitution.
/// </summary>
/// <remarks>
/// WPF's own default substitutes digits by Windows' regional format, whatever the app's language, so the choice is
/// always given explicitly.
/// </remarks>
public static class NativeDigits
{
    /// <summary>
    /// The application resources that hold the substitution, for windows, menus and tooltips, which do not inherit it
    /// from the main window.
    /// </summary>
    public const string CultureSourceKey = "AppNumberCultureSource";

    public const string CultureKey = "AppNumberCulture";

    public const string MethodKey = "AppNumberSubstitution";

    // WPF only substitutes digits for a culture with a country, which also names the digits, such as ar-SA for Arabic.
    private static readonly CultureInfo LatinDigitsCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// The substitution for text drawn by a control itself.
    /// </summary>
    public static NumberSubstitution Substitution { get; private set; } =
        new(NumberCultureSource.Override, LatinDigitsCulture, NumberSubstitutionMethod.European);

    /// <summary>
    /// Takes the selected language's digits and publishes them to the application's resources.
    /// </summary>
    public static void Apply(ResourceDictionary resources)
    {
        bool native = AppLanguages.UsesNativeDigits(Strings.Culture);
        CultureInfo culture = native ? SpecificCulture(Strings.Culture) : LatinDigitsCulture;
        NumberSubstitutionMethod method = native && !ReferenceEquals(culture, LatinDigitsCulture)
            ? NumberSubstitutionMethod.NativeNational
            : NumberSubstitutionMethod.European;
        Substitution = new NumberSubstitution(NumberCultureSource.Override, culture, method);
        resources[CultureSourceKey] = NumberCultureSource.Override;
        resources[CultureKey] = culture;
        resources[MethodKey] = method;
    }

    // The language's most likely country, such as ar-SA for ar, whose culture carries the language's digits. A
    // language without one keeps Latin digits.
    private static CultureInfo SpecificCulture(CultureInfo culture)
    {
        if (!culture.IsNeutralCulture && !culture.Equals(CultureInfo.InvariantCulture))
        {
            return culture;
        }

        try
        {
            CultureInfo specific = CultureInfo.CreateSpecificCulture(culture.Name);
            return specific.IsNeutralCulture || specific.Equals(CultureInfo.InvariantCulture) ? LatinDigitsCulture : specific;
        }
        catch (CultureNotFoundException)
        {
            return LatinDigitsCulture;
        }
    }
}
