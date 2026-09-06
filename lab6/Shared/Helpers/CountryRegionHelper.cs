using Shared.Enums;

namespace Shared.Helpers;

public static class CountryRegionHelper
{
    public static Region GetRegion(Country country)
    {
        return country switch
        {
            Country.Russia => Region.Ru,
            Country.France => Region.Eu,
            Country.Germany => Region.Eu,
            Country.Uae => Region.Asia,
            Country.India => Region.Asia,
            _ => Region.Eu
        };
    }

    public static string GetRegionCode(Region region)
    {
        return region switch
        {
            Region.Ru => "RU",
            Region.Eu => "EU",
            Region.Asia => "ASIA",
            _ => "EU"
        };
    }

    public static Region GetRegionByCode(string code)
    {
        return code switch
        {
            "RU"=> Region.Ru ,
            "EU" => Region.Eu,
            "ASIA" => Region.Asia,
            _ => Region.Eu
        };
    }
}