[System.Serializable]
public class RootDriverData
{
    public int count;
    public DriverEntry[] data;
    public string dataset;
    public DriverMetadata metadata;
    public string snapshot_at;
}

[System.Serializable]
public class DriverEntry
{
    public string date;
    public DriverInfo driver;
    public int driver_number;
    public int meeting_key;
    public int position;
    public int session_key;
}

[System.Serializable]
public class DriverInfo
{
    public string broadcast_name;
    public string country_code;
    public int driver_number;
    public string first_name;
    public string full_name;
    public string headshot_url;
    public string last_name;
    public int meeting_key;
    public string name_acronym;
    public int session_key;
    public string team_colour;
    public string team_name;
}

[System.Serializable]
public class DriverMetadata
{
    public string generated_at;
    public string provider;
    public string meeting_name;
    public string country_name;
    public int year;
    public int meeting_key;
    public string race_session_name;
    public int race_session_key;
    public int endpoint_count;
}
