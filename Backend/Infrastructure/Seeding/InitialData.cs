namespace Infrastructure.Seeding;

public static class InitialData
{
    public static readonly (string roleName, Guid? id)[]
        Roles =
        [
            ("Admin", null),
            ("Player", null),
        ];

    public static readonly (string name, string password, Guid? id, string[] roles)[]
        Users =
        [
            ("admin@admin.ee", "Admin.Password.1", null, ["Admin"]),
            ("player1@player.ee", "Player.Password.1", null, ["Player"]),
            ("player2@player.ee", "Player.Password.1", null, ["Player"]),
        ];
}
