using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace C2D;

internal sealed class MigrationRecord
{
    public string Id = "";
    public string Src = "";
    public string Dst = "";
    public string Bak = "";
    public long Bytes;
    public int Files;
    public string At = "";
    public string Status = ""; // linked | released | rolledback
    public int Line;
}

/// <summary>
/// 迁移台账：一行一条事件，追加写，按 id 折叠（后出现的字段覆盖先出现的）。
/// 这是程序唯一的本地文件，几十字节一条；丢了不影响已建好的链接，只丢历史。
/// </summary>
internal static class State
{
    /// <summary>测试用：把台账目录指到临时位置，避免污染真实数据。</summary>
    public static string? RootOverride;

    public static string Dir
    {
        get
        {
            if (!string.IsNullOrEmpty(RootOverride)) return RootOverride!;
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "伪迁移助手");
        }
    }

    public static string FilePath => Path.Combine(Dir, "state.jsonl");

    public static void Append(MigrationRecord r)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.AppendAllText(FilePath, ToLine(r) + "\n", new UTF8Encoding(false));
        }
        catch
        {
            // 记不上账不影响迁移本身
        }
    }

    public static List<MigrationRecord> Load()
    {
        var map = new Dictionary<string, MigrationRecord>(StringComparer.Ordinal);

        try
        {
            if (!File.Exists(FilePath)) return new List<MigrationRecord>();

            int lineNo = 0;
            foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                lineNo++;
                if (line.Length < 5 || line[0] != '{') continue;

                string id = Field(line, "id");
                if (id.Length == 0) continue;

                if (!map.TryGetValue(id, out var rec))
                {
                    rec = new MigrationRecord { Id = id };
                    map[id] = rec;
                }

                string v;
                if ((v = Field(line, "src")).Length > 0) rec.Src = v;
                if ((v = Field(line, "dst")).Length > 0) rec.Dst = v;
                if ((v = Field(line, "bak")).Length > 0) rec.Bak = v;
                if ((v = Field(line, "at")).Length > 0) rec.At = v;
                if ((v = Field(line, "status")).Length > 0) rec.Status = v;

                long bytes = Number(line, "bytes");
                if (bytes > 0) rec.Bytes = bytes;
                long files = Number(line, "files");
                if (files > 0) rec.Files = (int)files;

                rec.Line = lineNo;
            }
        }
        catch
        {
            // 文件坏了就当没有，绝不扫描
        }

        var list = new List<MigrationRecord>(map.Values.Count);
        foreach (var v in map.Values) list.Add(v);
        list.Sort((a, b) => b.Line.CompareTo(a.Line));
        return list;
    }

    private static string ToLine(MigrationRecord r)
    {
        return "{\"id\":\"" + Esc(r.Id) +
               "\",\"status\":\"" + Esc(r.Status) +
               "\",\"src\":\"" + Esc(r.Src) +
               "\",\"dst\":\"" + Esc(r.Dst) +
               "\",\"bak\":\"" + Esc(r.Bak) +
               "\",\"bytes\":" + r.Bytes +
               ",\"files\":" + r.Files +
               ",\"at\":\"" + Esc(r.At) + "\"}";
    }

    private static string Esc(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string Field(string line, string key)
    {
        string token = "\"" + key + "\":\"";
        int i = line.IndexOf(token, StringComparison.Ordinal);
        if (i < 0) return "";
        i += token.Length;

        var sb = new StringBuilder();
        while (i < line.Length)
        {
            char c = line[i];
            if (c == '\\' && i + 1 < line.Length)
            {
                sb.Append(line[i + 1]);
                i += 2;
                continue;
            }
            if (c == '"') break;
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    private static long Number(string line, string key)
    {
        string token = "\"" + key + "\":";
        int i = line.IndexOf(token, StringComparison.Ordinal);
        if (i < 0) return 0;
        i += token.Length;

        long v = 0;
        while (i < line.Length && line[i] >= '0' && line[i] <= '9')
        {
            v = v * 10 + (line[i] - '0');
            i++;
        }
        return v;
    }
}
