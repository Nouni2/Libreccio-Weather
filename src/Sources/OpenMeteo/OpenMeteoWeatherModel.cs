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

namespace LibreccioWeather.Core.Sources.OpenMeteo;

/// <summary>
/// A forecast model Open-Meteo can use, identified as in its API.
/// See https://open-meteo.com/en/docs/
/// </summary>
public sealed class OpenMeteoWeatherModel
{
    public static readonly OpenMeteoWeatherModel BestMatch = new("best_match");

    public static IReadOnlyList<OpenMeteoWeatherModel> All { get; } =
    [
        BestMatch,
        new("ecmwf_ifs"),
        new("ecmwf_ifs025"),
        new("ecmwf_aifs025_single"),
        new("cma_grapes_global"),
        new("bom_access_global"),
        new("ncep_gfs_seamless"),
        new("ncep_gfs_global"),
        new("ncep_hrrr_conus"),
        new("ncep_nbm_conus"),
        new("ncep_nam_conus"),
        new("ncep_aigfs025"),
        new("ncep_hgefs025_ensemble_mean"),
        new("jma_seamless"),
        new("jma_msm"),
        new("jma_gsm"),
        new("kma_seamless"),
        new("kma_ldps"),
        new("kma_gdps"),
        new("dwd_icon_seamless"),
        new("dwd_icon_global"),
        new("dwd_icon_eu"),
        new("dwd_icon_d2"),
        new("cmc_gem_seamless"),
        new("cmc_gem_gdps"),
        new("cmc_gem_rdps"),
        new("cmc_gem_hrdps"),
        new("cmc_gem_hrdps_west"),
        new("meteofrance_seamless"),
        new("meteofrance_arpege_world"),
        new("meteofrance_arpege_europe"),
        new("meteofrance_arome_france"),
        new("meteofrance_arome_france_hd"),
        new("italia_meteo_arpae_icon_2i"),
        new("metno_seamless"),
        new("metno_nordic"),
        new("knmi_seamless"),
        new("knmi_harmonie_arome_europe"),
        new("knmi_harmonie_arome_netherlands"),
        new("dmi_seamless"),
        new("dmi_harmonie_arome_europe"),
        new("ukmo_seamless"),
        new("ukmo_global_deterministic_10km"),
        new("ukmo_uk_deterministic_2km"),
        new("meteoswiss_icon_seamless"),
        new("meteoswiss_icon_ch1"),
        new("meteoswiss_icon_ch2"),
        new("geosphere_seamless"),
        new("geosphere_arome_austria"),
        new("chmi_aladin_seamless"),
        new("chmi_aladin_central_europe_2km"),
        new("chmi_aladin_cz_1km"),
    ];

    private OpenMeteoWeatherModel(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public static OpenMeteoWeatherModel? GetInstance(string value)
    {
        return All.FirstOrDefault(model => model.Id == value);
    }

    public override string ToString()
    {
        return Id;
    }
}
