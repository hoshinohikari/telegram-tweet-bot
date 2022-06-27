using Microsoft.Data.Sqlite;

namespace Twitter_Bot;

public class Sqlite
{
    private readonly SqliteConnection _sql;

    public Sqlite(string path)
    {
        _sql = new SqliteConnection($"Data Source={path};Cache=Shared");

        _sql.Open();

        var command = _sql.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS subscription(
	""id""              INTEGER NOT NULL,
	""tg_chat_id""      TEXT NOT NULL,
	""tw_user_id""      INTEGER NOT NULL,
	""last_tweet_id""   INTEGER NOT NULL,
	""sub_kind""        TEXT NOT NULL,
    ""sub_timezone""    TEXT NOT NULL,
	PRIMARY             KEY(""id"")
);
";
        command.ExecuteNonQuery();
    }

    ~Sqlite()
    {
        _sql.Close();
    }

    private static List<long> SplitString2LongList(string s)
    {
        var gets = s.Split(',');

        return gets.Select(long.Parse).ToList();
    }

    private static string LongList2String(IReadOnlyList<long> ll)
    {
        var s = "";
        for (var i = 0; i < ll.Count; i++)
        {
            if (i != 0)
                s += ",";
            s += ll[i].ToString();
        }

        return s;
    }

    public async Task AddSubAsync(long sub, long chatId, long kind)
    {
        var isExist = false;
        var command = _sql.CreateCommand();
        command.CommandText = @$"SELECT * FROM subscription WHERE tw_user_id = {sub};";
        var chats = "";
        var subKinds = "";
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (reader.Read())
            {
                chats = reader.GetString(1);
                subKinds = reader.GetString(4);
                isExist = true;
            }
        }

        var maxId = 0;
        if (!isExist)
        {
            command.CommandText = @"SELECT MAX(id) FROM subscription";
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    if (await reader.IsDBNullAsync(0))
                        continue;
                    maxId = reader.GetInt32(0);
                }
            }

            command.CommandText =
                @$"INSERT INTO subscription VALUES ({maxId + 1}, '{chatId}', {sub}, 0, '{kind}', '' );";
            command.ExecuteNonQuery();
        }
        else
        {
            var chatList = SplitString2LongList(chats);
            if (chatList.IndexOf(chatId) != -1)
                return;
            command.CommandText =
                @$"UPDATE subscription SET tg_chat_id = '{chats},{chatId}', sub_kind = '{subKinds},{kind}' WHERE tw_user_id = {sub};";
            command.ExecuteNonQuery();
        }
    }

    public async Task DelSubAsync(long sub, long chatId)
    {
        var isExist = false;
        var command = _sql.CreateCommand();
        command.CommandText = @$"SELECT * FROM subscription WHERE tw_user_id = {sub};";
        var chats = "";
        var subKinds = "";
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (reader.Read())
            {
                chats = reader.GetString(1);
                subKinds = reader.GetString(4);
                isExist = true;
            }
        }

        if (!isExist)
            return;

        var chatList = SplitString2LongList(chats);
        var chatIndex = chatList.IndexOf(chatId);
        var subKindList = SplitString2LongList(subKinds);

        if (chatIndex != -1)
        {
            if (chatList.Count == 1)
            {
                command.CommandText = @$"DELETE FROM subscription WHERE tw_user_id = {sub};";
                command.ExecuteNonQuery();
            }
            else
            {
                chatList.RemoveAt(chatIndex);
                subKindList.RemoveAt(chatIndex);
                command.CommandText =
                    @$"UPDATE subscription SET tg_chat_id = '{LongList2String(chatList)}', sub_kind = '{LongList2String(subKindList)}' WHERE tw_user_id = {sub};";
                command.ExecuteNonQuery();
            }
        }
    }

    public async Task<List<Sublist>> GetSubListAsync()
    {
        List<Sublist> subList = new();
        var command = _sql.CreateCommand();
        command.CommandText = @"SELECT tw_user_id, last_tweet_id, tg_chat_id, sub_kind FROM subscription";
        await using var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
            var chatid = SplitString2LongList(reader.GetString(2));
            var subkind = SplitString2LongList(reader.GetString(3));
            subList.Add(new Sublist
                { Id = reader.GetInt64(0), Sinceid = reader.GetInt64(1), ChatId = chatid, SubKind = subkind });
        }

        return subList;
    }

    public void UpdateLastTweet(long twUserId, long? lastTweetId)
    {
        var command = _sql.CreateCommand();
        command.CommandText =
            @$"UPDATE subscription SET last_tweet_id = {lastTweetId} WHERE tw_user_id = {twUserId}";
        command.ExecuteNonQuery();
    }

    public struct Sublist
    {
        public long Id;
        public long Sinceid;
        public List<long> ChatId;
        public List<long> SubKind;
    }
}