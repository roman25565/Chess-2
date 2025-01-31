public class FirebasePlayerData
{
    public string ID;
    public string Name;
    public int Elo;
    

    public FirebasePlayerData(string ID, string Name, int Elo)
    {
        this.ID = ID;
        this.Name = Name;
        this.Elo = Elo;
    }

    public static FirebasePlayerData CreateFirebasePlayerData(string id)
    {
        return new FirebasePlayerData(id, "BUGAGAGA", 500);
    }
}