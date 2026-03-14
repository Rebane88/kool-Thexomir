namespace Infrastructure.Seeding;

public static class InitialData
{
    public static readonly (string roleName, Guid id)[] Roles =
    [
        ("Admin", new Guid("00000000-0000-0000-0000-000000000001")),
        ("Player", new Guid("00000000-0000-0000-0000-000000000002"))
    ];

    public static readonly (string name, string password, string[] roles)[] Users =
    [
        ("admin@admin.ee", "Admin1!", ["Admin", "Player"]),
        ("player1@player.ee", "Player1!", ["Player"]),
        ("player2@player.ee", "Player1!", ["Player"])
    ];
}
