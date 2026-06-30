using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisualGGPK3;

internal sealed class FavoritesData {
	public List<FavoriteGroup> Groups { get; set; } = [];
	public List<FavoriteEntry> Entries { get; set; } = [];
}

internal sealed class FavoriteGroup {
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ParentId { get; set; }
	public int Order { get; set; }
}

internal sealed class FavoriteEntry {
	public string Path { get; set; } = "";
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? GroupId { get; set; }
	public int Order { get; set; }
}

internal static class FavoritesStore {
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	private static string JsonPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"VisualGGPK3",
		"favorites.json");

	private static string LegacyPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"VisualGGPK3",
		"favorites.txt");

	public static FavoritesData LoadData() {
		try {
			MigrateLegacyIfNeeded();
			if (!File.Exists(JsonPath))
				return new FavoritesData();
			var json = File.ReadAllText(JsonPath);
			return JsonSerializer.Deserialize<FavoritesData>(json, JsonOptions) ?? new FavoritesData();
		} catch {
			return new FavoritesData();
		}
	}

	public static bool Contains(string path) {
		var normalized = FavoritePaths.Normalize(path);
		return LoadData().Entries.Any(e => FavoritePaths.Equals(e.Path, normalized));
	}

	public static void Add(string path, string? groupId = null) {
		var normalized = FavoritePaths.Normalize(path);
		if (string.IsNullOrEmpty(normalized))
			return;
		var data = LoadData();
		if (data.Entries.Any(e => FavoritePaths.Equals(e.Path, normalized)))
			return;
		if (groupId is not null && data.Groups.All(g => g.Id != groupId))
			groupId = null;
		var order = NextEntryOrder(data, groupId);
		data.Entries.Add(new FavoriteEntry { Path = normalized, GroupId = groupId, Order = order });
		Save(data);
	}

	public static void Remove(string path) {
		var normalized = FavoritePaths.Normalize(path);
		var data = LoadData();
		data.Entries.RemoveAll(e => FavoritePaths.Equals(e.Path, normalized));
		Save(data);
	}

	public static string CreateGroup(string name, string? parentId = null) {
		name = name.Trim();
		if (string.IsNullOrEmpty(name))
			name = "New folder";
		var data = LoadData();
		if (parentId is not null && data.Groups.All(g => g.Id != parentId))
			parentId = null;
		var group = new FavoriteGroup {
			Id = Guid.NewGuid().ToString("N"),
			Name = name,
			ParentId = parentId,
			Order = NextGroupOrder(data, parentId)
		};
		data.Groups.Add(group);
		Save(data);
		return group.Id;
	}

	public static void RenameGroup(string id, string name) {
		name = name.Trim();
		if (string.IsNullOrEmpty(name))
			return;
		var data = LoadData();
		var group = data.Groups.FirstOrDefault(g => g.Id == id);
		if (group is null)
			return;
		group.Name = name;
		Save(data);
	}

	public static void DeleteGroup(string id) {
		var data = LoadData();
		var group = data.Groups.FirstOrDefault(g => g.Id == id);
		if (group is null)
			return;
		var parentId = group.ParentId;
		foreach (var child in data.Groups.Where(g => g.ParentId == id).ToList())
			child.ParentId = parentId;
		foreach (var entry in data.Entries.Where(e => e.GroupId == id))
			entry.GroupId = parentId;
		data.Groups.RemoveAll(g => g.Id == id);
		Save(data);
	}

	public static void MoveEntry(string path, string? groupId) {
		var normalized = FavoritePaths.Normalize(path);
		var data = LoadData();
		var entry = data.Entries.FirstOrDefault(e => FavoritePaths.Equals(e.Path, normalized));
		if (entry is null)
			return;
		if (groupId is not null && data.Groups.All(g => g.Id != groupId))
			groupId = null;
		entry.GroupId = groupId;
		entry.Order = NextEntryOrder(data, groupId);
		Save(data);
	}

	public static void MoveGroup(string groupId, string? parentId) {
		var data = LoadData();
		var group = data.Groups.FirstOrDefault(g => g.Id == groupId);
		if (group is null)
			return;
		if (parentId == groupId || IsDescendantGroup(data, groupId, parentId))
			return;
		if (parentId is not null && data.Groups.All(g => g.Id != parentId))
			parentId = null;
		group.ParentId = parentId;
		group.Order = NextGroupOrder(data, parentId);
		Save(data);
	}

	private static bool IsDescendantGroup(FavoritesData data, string ancestorId, string? candidateParentId) {
		if (candidateParentId is null)
			return false;
		for (var id = candidateParentId; id is not null;) {
			if (id == ancestorId)
				return true;
			id = data.Groups.FirstOrDefault(g => g.Id == id)?.ParentId;
		}
		return false;
	}

	private static int NextGroupOrder(FavoritesData data, string? parentId) {
		var max = data.Groups.Where(g => g.ParentId == parentId).Select(g => g.Order).DefaultIfEmpty(-1).Max();
		return max + 1;
	}

	private static int NextEntryOrder(FavoritesData data, string? groupId) {
		var max = data.Entries.Where(e => e.GroupId == groupId).Select(e => e.Order).DefaultIfEmpty(-1).Max();
		return max + 1;
	}

	private static void MigrateLegacyIfNeeded() {
		if (!File.Exists(LegacyPath) || File.Exists(JsonPath))
			return;
		try {
			var data = new FavoritesData();
			var order = 0;
			foreach (var line in File.ReadAllLines(LegacyPath)) {
				var path = FavoritePaths.Normalize(line);
				if (string.IsNullOrEmpty(path) || path.StartsWith('#'))
					continue;
				if (data.Entries.Any(e => FavoritePaths.Equals(e.Path, path)))
					continue;
				data.Entries.Add(new FavoriteEntry { Path = path, Order = order++ });
			}
			Save(data);
			try {
				File.Move(LegacyPath, LegacyPath + ".bak", overwrite: true);
			} catch {
				// ignore backup failure
			}
		} catch {
			// ignore migration errors
		}
	}

	private static void Save(FavoritesData data) {
		try {
			var dir = Path.GetDirectoryName(JsonPath)!;
			Directory.CreateDirectory(dir);
			File.WriteAllText(JsonPath, JsonSerializer.Serialize(data, JsonOptions));
		} catch {
			// ignore persistence errors
		}
	}
}
