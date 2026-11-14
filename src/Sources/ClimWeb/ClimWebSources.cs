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

using LibreccioWeather.Core.Settings;

namespace LibreccioWeather.Core.Sources.ClimWeb;

public sealed class AnamBfService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "anambf", "BF", "ANAM-BF", "https://www.meteoburkina.bf/", "Agence Nationale de la Météorologie du Burkina Faso",
    null, "settings_weather_source_anam_bf_instance")
{
}

public sealed class AnametService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "anamet", "TG", "ANAMET", "https://www.anamet-togo.com/", "Agence Nationale de la Météorologie du Togo",
    null, "settings_weather_source_anamet_instance")
{
}

public sealed class DccmsService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "dccms", "MW", "DCCMS", "https://www.metmalawi.gov.mw/", "Department of Climate Change and Meteorological Services",
    "33", "settings_weather_source_dccms_instance")
{
}

public sealed class DmnNeService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "dmnne", "NE", "DMN", "https://www.niger-meteo.ne/", "Direction de la Météorologie Nationale du Niger",
    "51", "settings_weather_source_dmn_ne_instance")
{
}

public sealed class DwrGmService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "dwrgm", "GM", "DWR", "https://meteogambia.org/", "Department of Water Resources",
    null, "settings_weather_source_dwr_gm_instance")
{
}

public sealed class EthioMetService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "ethiomet", "ET", "EMI", "https://www.ethiomet.gov.et/", "Ethiopian Meteorological Institute",
    "212", "settings_weather_source_ethiomet_instance")
{
}

public sealed class GMetService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "gmet", "GH", "GMet", "https://www.meteo.gov.gh/", "Ghana Meteorological Agency",
    null, "settings_weather_source_gmet_instance")
{
}

public sealed class IgebuService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "igebu", "BI", "IGEBU", "https://www.igebu.bi/", "Institut Géographique du Burundi",
    null, "settings_weather_source_igebu_instance")
{
}

public sealed class InmgbService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "inmgb", "GW", "INM-GB", "https://www.meteoguinebissau.org/", "Instituto Nacional de Meteorologia da Guiné-Bissau",
    null, "settings_weather_source_imngb_instance")
{
}

public sealed class MaliMeteoService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "malimeteo", "ML", "Mali-Météo", "https://malimeteo.ml/", "Agence Nationale de la Météorologie du Mali",
    null, "settings_weather_source_mali_meteo_instance", omitRepeatedCountry: true)
{
}

public sealed class MeteoBeninService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "meteobenin", "BJ", "Météo Bénin", "https://www.meteobenin.bj/", "Agence Nationale de la Météorologie du Bénin",
    "105", "settings_weather_source_meteo_benin_instance", omitRepeatedCountry: true)
{
}

public sealed class MeteoTchadService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "meteotchad", "TD", "Météo Tchad", "https://www.meteotchad.org/", "Agence Nationale de la Météorologie du Tchad",
    "65", "settings_weather_source_meteo_tchad_instance", omitRepeatedCountry: true)
{
}

public sealed class MettelsatService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "mettelsat", "CD", "Mettelsat RDC", "https://www.meteordcongo.cd/", "Agence Nationale de Météorologie et de Télédétection par Satellite",
    null, "settings_weather_source_mettelsat_instance")
{
}

public sealed class MsdZwService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "msdzw", "ZW", "MSD", "https://www.weatherzw.org.zw/", "Meteorological Services Department of Zimbabwe",
    null, "settings_weather_source_msd_zw_instance")
{
}

public sealed class SmaScService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "smasc", "SC", "SMA", "https://www.meteo.sc/", "Seychelles Meteorological Authority",
    "305", "settings_weather_source_sma_sc_instance")
{
}

public sealed class SmaSuService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "smasu", "SD", "SMA", "https://meteosudan.sd/", "Sudan Meteorological Authority",
    null, "settings_weather_source_sma_su_instance")
{
}

public sealed class SsmsService(HttpClient client, ConfigStore config) : ClimWebService(client, config,
    "ssms", "SS", "SSMS", "https://meteosouthsudan.com.ss/", "South Sudan Meteorological Services",
    null, "settings_weather_source_ssms_instance")
{
}
