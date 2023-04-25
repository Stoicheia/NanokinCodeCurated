using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Anjin.Core.Flags;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Util.Odin.Attributes;
using WanzyeeStudio.Json;
using Debug = UnityEngine.Debug;
using Flags = Anjin.Core.Flags.Flags;
using g = ImGuiNET.ImGui;

namespace SaveFiles {

	/// <summary>
	/// Info about why we're setting the file.
	/// </summary>
	public enum SetSaveAction {
		New,			// Creating a new file
		Load,			// Loading an existing file on disk
		CopyCurrent,	// Switching to a copy of the previous file
	}

	public class SaveManager : StaticBoy<SaveManager>, IDebugDrawer
	{
		public const string DBG_NAME        = "Saves";
		public const string DEBUG_FILE_NAME = "debug";
		public const int    MAX_SAVE_SLOTS  = 10;

		private const string FILE_PREFIX  = "save-";
		private const string FILE_POSTFIX = ".json";

		private List<string> _cachedSaveNames;

		[ShowInPlay]
		public static SaveData     current       { get; private set; }

		[ShowInPlay, NotNull]
		public static SaveData     debugData     { get; private set; }

		[ShowInPlay]
		public static bool HasData => current != null;

		[ShowInPlay]
		public static int          SaveDataCount { get; private set; }

		public static Action<SaveData, SetSaveAction> onCurrentChanged;


		[ShowInPlay] public static List<SaveFileID>            SaveIDsOnDisk;
		[ShowInPlay] public static Dictionary<int, SaveFileID> NumberedFilesOnDisk;

		[ShowInPlay] public static List<SaveData>              LoadedFilesOnDisk;
		//[ShowInPlay] public static List<SaveData> AllRuntimeFiles;

		private static AsyncLazy                 _loadTask;


		private static bool _initialized = false;



		public static void Init()
		{
			if (_initialized) return;
			_initialized = true;

			/*AllNumberedFiles = new List<SaveData>();
			AllNamedFiles    = new List<SaveData>();*/

			SaveIDsOnDisk       = new List<SaveFileID>();
			LoadedFilesOnDisk   = new List<SaveData>();
			NumberedFilesOnDisk = new  Dictionary<int, SaveFileID>(MAX_SAVE_SLOTS);

			/*for (int i = 0; i < MAX_SAVE_SLOTS; i++)
				_numberedFileTable.Add(i, null);*/

			SaveDataCount = 0;
		}

		public override async void Awake()
		{
			base.Awake();

			DebugSystem.Register(this);

			/*Init();
			_loadTask = StartupAsync().ToAsyncLazy();
			await _loadTask;*/
		}

		public static async UniTask InitializeThroughGameController()
		{
			Init();
			await StartupAsync();
		}

		private static async UniTask StartupAsync()
		{
			// NOTE: Moved all of this outside of the editor block

			#if UNITY_EDITOR
			// See if we have the debug file on the disk
			RefreshIDsOnDisk();

			foreach (SaveFileID id in SaveIDsOnDisk) {
				if (id.IsValid() && id.isNamed && id.name == DEBUG_FILE_NAME) {
					if(TryLoad(id, out SaveData data)) {
						debugData = data;
						break;
					}
				}
			}

			// If we don't have the debug file on disk, we should initialize it
			if (debugData == null) {
				Live.Log("Debug save file does not exist on disk. Creating it.");
				debugData = CreateFile(DEBUG_FILE_NAME);
				debugData.SetMaxedData();
				Write(debugData);
			} else {
				Live.Log("Debug save file loaded.");
			}

			Set(debugData);

			#else

			//await Load(GetSavePath(GameOptions.current.default_savefile));
			//LoadAllAsync().Forget();

			// NOTE: we only load the files, we don't set anything on startup in the build
			//LoadAll();

			#endif
		}


		private void Start()
		{
			// NOTE(CL) No idea if the debug console is even usable currently

			/*DebugConsole.AddCommand("savefile", (digester, io) => io.output.Add(current));
			DebugConsole.AddCommand("save",     (digester, io) => current.Write());
			DebugConsole.AddCommand("load",     (digester, io) => current.LoadAsync().Forget());*/
			// DebugConsole.AddCommand("savefiles", (digester, io) => io.output.AddRange(Current));
		}

		public void OnDisable()
		{
			SaveCurrent(devWrite: true);
		}

		private void Update()
		{
			if (HasData) {
				current.PlayTime += Time.deltaTime;
			}
		}

		/// <summary>
		/// Get the current save, once it is ready to be used.
		/// </summary>
		/// <returns></returns>
		public static async UniTask<SaveData> GetCurrentAsync()
		{
			#if UNITY_EDITOR
			//await _loadTask;
			#endif
			return current;
		}

		//=============================================================================================================
		//	CREATION
		//=============================================================================================================

		public static SaveData CreateRuntime() => new SaveData();

		public static SaveData CreateFile(SaveFileID? ID, bool write = false)
		{
			if (!ID.HasValue || !ID.Value.isNamed && ID.Value.index < 0) {
				// If no ID, OR the ID is less than 0, default to the next free numbered file.

				int index = CountNumberedFiles();

				if (!GetSavePath(index, out string path)) return null;

				SaveData save = new SaveData(index, path);

				if (write) Write(save);

				LoadedFilesOnDisk.Add(save);

				return save;
			} else {

				SaveFileID id = ID.Value;

				if (!GetSavePath(id, out string path)) return null;

				SaveData save = new SaveData(id, path);

				if (write) Write(save);

				LoadedFilesOnDisk.Add(save);

				return save;
			}
		}


		//=============================================================================================================
		//	SWITCHING
		//=============================================================================================================


		/// <summary>
		/// Change the current active save data.
		/// </summary>
		/// <param name="save"></param>
		public static void Set(SaveData save, SetSaveAction action = SetSaveAction.New)
		{
			current = save;
			GameController.Live.OnChangeSavedata(save, action);
			onCurrentChanged?.Invoke(save, action);

		}


		//=============================================================================================================
		//	WRITING
		//=============================================================================================================

		/// <summary>
		/// Write a savefile to disk.
		/// The path should be stored in the savedata already.
		/// </summary>
		/// <param name="save"></param>
		/// <param name="origin">Where the save originated from.</param>
		public static void Write(SaveData save, SaveData.SaveOrigin origin = SaveData.SaveOrigin.None)
		{
			save.Origin = origin;

			string path = save.filePath;

			DirectoryInfo dir = new FileInfo(path).Directory;
			if (dir != null && !dir.Exists)
				dir.Create();

			using (var file = new StreamWriter(path, false))
			{
				file.Write(JsonConvert.SerializeObject(save,
													   Formatting.Indented,
													   new Vector2Converter(),
													   new Vector3Converter()));
			}
		}

		/// <summary>
		/// Write the current savefile to the disk.
		/// </summary>
		public static void SaveCurrent(SaveData.SaveOrigin origin = SaveData.SaveOrigin.None, bool devWrite = false)
		{
			if (devWrite && !GameOptions.current.autosave_devmode)
				return;

			if (current == null)
			{
				Debug.LogError("Cannot write because we do not have a savefile loaded currently.");
				return;
			}

			UpdateSaveWithGlobalData(current);
			current.LastTimeSaved = current.PlayTime;


			// Write to disk
			Write(current, origin);
		}

		/*public static bool SaveCurrent(SaveData data)
		{
			if (!data.ID.HasValue || !data.ID.Value.IsValid()) return false;

			Write(data);
			return true;
		}*/

		public static bool CopySaveWithNewID(SaveFileID newID, SaveData sourceData, out SaveData newData, bool writeNewSave = false, bool storeLastSavedTime = false)
		{
			newData = null;

			if (sourceData == null) return false;
			if (!newID.IsValid()) return false;
			if (!GetSavePath(newID, out string path)) return false;

			string copyString = JsonConvert.SerializeObject(sourceData,
											  Formatting.Indented,
											  new Vector2Converter(),
											  new Vector3Converter());

			SaveData _newData = new SaveData(newID, path);

			try
			{
				JsonConvert.PopulateObject(copyString, _newData);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to create copy of save '{sourceData.ID}'.");
				Debug.LogException(e);

				return false;
			}

			newData          = _newData;

			newData.LastTimeSaved = newData.PlayTime;

			if(writeNewSave)
				Write(newData);

			LoadedFilesOnDisk.Add(newData);

			return true;
		}

		public static void UpdateSaveWithGlobalData(SaveData data)
		{
			// Copy GameFlags state
			data.FlagValues.Clear();
			if (Flags.Exists)
			{
				foreach (FlagStateBase flag in Flags.Live.AllFlags)
				{
					data.FlagValues[flag.DefBase.Name] = flag.GetValue();
				}
			}
		}

		//=============================================================================================================
		//	READING
		//=============================================================================================================


		/// <summary>
		/// Read data from the disk into the specified (savable) save data.
		/// (The data must have a valid file ID)
		/// </summary>
		/// <param name="data">The save data to read from disk.</param>
		/// <returns>If the read was successful</returns>
		public static bool Read(SaveData data)
		{
			if (!data.ID.HasValue || !data.ID.Value.IsValid()) {
				Live.LogError($"Read(): ID for data was null or invalid ({data.ID ?? "null"})");
				return false;
			}

			data.Reset();

			if (!GetSavePath(data.ID.Value, out string path)) {
				Live.LogError($"Read(): Failed to get path for ID: ({data.ID.Value})");
				return false;
			}

			if (!File.Exists(path)) {
				Live.LogError($"Read(): File at path '{path}' does not exist.");
				return false;
			}

			using (var fileStreamReader = new StreamReader(path))
			{
				string jsonContent = fileStreamReader.ReadToEnd();
				try
				{
					JsonConvert.PopulateObject(jsonContent, data);
				}
				catch (Exception e)
				{
					Debug.LogError($"Save file at '{Path.GetFileName(path)}\n{path}' failed to load.");
					Debug.LogException(e);

					return false;
				}
			}

			return true;
		}

		public static async UniTask<bool> ReadAsync(SaveData data)
		{
			await UniTask.SwitchToThreadPool();

			if (!data.ID.HasValue || !data.ID.Value.IsValid()) {
				Live.LogError($"ReadAsync(): ID for data was null or invalid ({data.ID ?? "null"})");
				return false;
			}

			data.Reset();

			if (!GetSavePath(data.ID.Value, out string path)) {
				Live.LogError($"ReadAsync(): Failed to get path for ID: ({data.ID.Value})");
				return false;
			}

			if (!File.Exists(path)) {
				Live.LogError($"ReadAsync(): File at path '{path}' does not exist.");
				return false;
			}

			using (var fileStreamReader = new StreamReader(path))
			{
				string jsonContent = await fileStreamReader.ReadToEndAsync();
				try
				{
					JsonConvert.PopulateObject(jsonContent, data);
				}
				catch (Exception e)
				{
					Debug.LogError($"Save file at '{Path.GetFileName(path)}\n{path}' failed to load.");
					Debug.LogException(e);

					return false;
				}
			}

			await UniTask.SwitchToMainThread();

			return true;
		}

		public static bool TryLoad(SaveFileID id, out SaveData data)
		{
			data = null;

			if (!GetSavePath(id, out string path)) return false;

			data = new SaveData(id, path);

			data.Reset();
			if (!Read(data)) {
				Live.LogError("TryLoad(id): Failed to read save file.");
				return false;
			}

			return true;
		}

		public static bool TryLoad([NotNull] string path, out SaveData data)
		{
			data = null;

			if (!path.Contains(FILE_PREFIX) || !path.Contains(FILE_POSTFIX)) {
				Live.LogError($"TryLoad(path): Path does not contain prefix or postfix ({path})");
				return false;
			}

			if (!FilenameToID(Path.GetFileName(path), out SaveFileID id))
				return false;

			data = new SaveData(id, path);
			data.Reset();

			if (!Read(data)) {
				Live.LogError("TryLoad(path): Failed to read save file.");
				return false;
			}

			return true;
		}

		public static async UniTask<(bool ok, SaveData instance)> TryLoadAsync(SaveFileID id)
		{
			await GameAssets.loadTask;

			if (!GetSavePath(id, out string path)) return (false, null);

			SaveData data = new SaveData(id, path);

			data.Reset();

			if (!(await ReadAsync(data))) {
				Live.LogError("TryLoadAsync(id): Failed to read save file.");
				return (false, data);
			}

			return (true, data);
		}

		public static void LoadAllFilesOnDisk()
		{
			LoadedFilesOnDisk.Clear();
			/*AllNumberedFiles.Clear();
			AllNamedFiles.Clear();*/

			foreach (string path in Directory.EnumerateFiles(Application.persistentDataPath))
			{
				string fileName = Path.GetFileName(path);

				if (fileName.StartsWith(FILE_PREFIX) && fileName.EndsWith(FILE_POSTFIX)) {
					if (TryLoad(path, out SaveData data)) {
						LoadedFilesOnDisk.Add(data);
					}
					/*var save = new SaveData(path);
					Load(save);
					AllNumberedFiles.Add(save);*/
				}
			}
		}

		public static void RefreshIDsOnDisk()
		{
			SaveIDsOnDisk.Clear();

			foreach (string path in Directory.EnumerateFiles(Application.persistentDataPath))
			{
				string fileName = Path.GetFileName(path);

				if (fileName.StartsWith(FILE_PREFIX) && fileName.EndsWith(FILE_POSTFIX)) {

					if (!FilenameToID(Path.GetFileName(path), out SaveFileID id))
						continue;

					SaveIDsOnDisk.Add(id);

					if (!id.isNamed) {
						if (!NumberedFilesOnDisk.ContainsKey(id.index))
							NumberedFilesOnDisk[id.index] = id;
					}
				}
			}
		}

		public static async UniTask<SaveData> CreateOrLoadFileAsync(SaveFileID id, bool writeIfNotExisting = false)
		{
			if (!GetSavePath(id, out string path)) return null;

			if (!TryLoad(id, out SaveData existing)) {
				return existing;
			}

			return CreateFile(id, writeIfNotExisting);
		}

		public static SaveData CreateOrLoadFile(SaveFileID id, bool writeIfNotExisting = false)
		{
			if (!GetSavePath(id, out string path)) return null;

			if (!TryLoad(id, out SaveData existing)) {
				return existing;
			}

			return CreateFile(id, writeIfNotExisting);
		}

		public static void DeleteFile(SaveData save)
		{
			if (!save.ID.HasValue || !save.ID.Value.IsValid() || StringExtensions.IsNullOrWhitespace(save.filePath)) {
				Live.Log($"DeleteFile(): Tried to delete a save file with an invalid ID ({save.ID ?? "null"})");
				return;
			}

			string path = save.filePath;
			File.Delete(path);

			LoadedFilesOnDisk.Remove(save);

			if (save == current) {
				Live.LogError("WARNING: Deleted the current save! No idea what happens now!");
			}
		}

		/*public static void DeleteSave(int key, SaveData save)
		{
			string path = save.filePath;
			File.Delete(path);

			//AllSaveFiles.RemoveAll(x => x.saveName == save.saveName);

			_numberedFileTable[key] = null;

			--SaveDataCount;
		}*/

		//=============================================================================================================
		//	UTILITY
		//=============================================================================================================

		/// <summary>
		/// Get the full path for a save by name.
		/// The directory's path is OS dependant.
		/// </summary>
		/// <param name="id">The ID</param>
		/// <param name="path">The output path (if the ID is valid)</param>
		/// <returns>If the provided ID results in a valid path.</returns>
		public static bool GetSavePath(SaveFileID id, out string path)
		{
			if (!id.IsValid()) {
				path = "";
				return false;
			}

			path = Path.Combine(Application.persistentDataPath, $"{FILE_PREFIX}{(id.isNamed ? id.name : id.index.ToString())}{FILE_POSTFIX}");
			return true;
		}

		public static bool FilenameToID(string fileName, out SaveFileID id)
		{
			id = new SaveFileID();

			string idString = fileName.Replace(FILE_PREFIX, "").Replace(FILE_POSTFIX, "");
			if (int.TryParse(idString, out int index)) {
				id = index;
				return true;
			}

			id = idString;
			return true;
		}

		public static int CountNumberedFiles()
		{
			int number = 0;
			for (int i = 0; i < LoadedFilesOnDisk.Count; i++) {
				var file = LoadedFilesOnDisk[i];
				if (file.ID.HasValue && file.ID.Value.IsValid() && !file.ID.Value.isNamed)
					number++;
			}

			return number;
		}

		//public static SaveData GetSaveFileEntry(int index) => (_numberedFileTable.ContainsKey(index) ? _numberedFileTable[index] : null);

		public static bool SaveExists(SaveFileID id)
		{
			if (!GetSavePath(id, out var path)) return false;
			return File.Exists(path);
		}

		//public static bool HasSave(string name) => File.Exists(GetSavePath(name));

		/// <summary>
		/// Enumerate all save names on the disk.
		/// Note: these are save NAMES, not the full path.
		/// For example, instead of "C:\Users\Bob\AppData\LocalLow\Anjin\Nanokin\save-1.json",
		/// it would be simply "save-1".
		/// </summary>
		[NotNull]
		public static List<string> EnumerateSaves()
		{
			List<string> files = new List<string>();

			files.Clear();
			files = Directory.EnumerateFiles(Application.persistentDataPath)
							 .Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(FILE_PREFIX))
							 .Where(f => !f.Contains(DEBUG_FILE_NAME))
							 .OrderBy(f => f)
							 .Select(f => Path.GetFileName(f)
											  .Replace(FILE_PREFIX,  "")
											  .Replace(FILE_POSTFIX, ""))
							 .ToList();

			return files;
		}


		//=============================================================================================================
		//	LUA API
		//=============================================================================================================
		[LuaGlobalFunc]
		public static void party_heal()
		{
			current?.HealParty();
		}


		//=============================================================================================================
		//	DEBUG/EDITOR
		//=============================================================================================================

		#if UNITY_EDITOR
		[MenuItem("Nanokin/Open Save File Directory")]
		public static void OpenSaveFileDirectory()
		{
			Process.Start(Application.persistentDataPath);
		}
		#endif

		[DebugRegisterGlobals]
		public static void RegisterMenu()
		{
			DebugSystem.RegisterMenu(DBG_NAME);
		}

		private int        _dbgSelectedSaveFile = 0;
		private SaveFileID _newFileID           = SaveFileID.DefaultIndexed;
		private static SaveData   _dataDeleting = null;

		private static Dictionary<SaveFileID, SaveData> _debugLoadedFromDisk = new Dictionary<SaveFileID, SaveData>();

		public void OnLayout(ref DebugSystem.State state)
		{
			/*async UniTaskVoid UseDebugSave()
			{
				SaveData save = await CreateOrLoadAsync(DEBUG_FILE_NAME);
				save.SetMaxedData();

				Set(save);
			}

			async UniTaskVoid UseSave(string name)
			{
				(bool ok, SaveData data) = await Load(name);
				if (ok)
				{
					Set(current);
				}
			}*/

			if (state.Begin(DBG_NAME))
			{

				//g.BeginChild("list", new Vector2(86, 0), true);

				if (_cachedSaveNames == null)
					_cachedSaveNames = EnumerateSaves();




				AImgui.Text("New File:\n------------------------------------------", ColorsXNA.Goldenrod);

				SaveFileID.OnImgui(ref _newFileID);

				g.SameLine();

				if (g.Button("New File On Disk")) {
					CreateFile(_newFileID, true);
				}

				g.SameLine();

				if (current != null && g.Button("Save current to file")) {
					UpdateSaveWithGlobalData(current);
					CopySaveWithNewID(_newFileID, current, out SaveData newData, true);
				}

				//g.SameLine();

				AImgui.VSpace(24);

				//AImgui.TextExt("Loaded Files:", ColorsXNA.Goldenrod, 2);
				AImgui.Text("Loaded Files:\n------------------------------------------", ColorsXNA.Goldenrod);

				if (HasData) {
					if (g.CollapsingHeader($"Current File (ID: {current.ID ?? "(null ID)"})")) {

						g.Indent(12);

						g.PushID("current");
						if (g.Button("Unload")) {
							current = null;
						}

						ImGuiDrawSaveData(current);
						g.Unindent(12);
						g.PopID();
					}
				} else {
					g.Text($"No save file loaded!");
				}


				if (g.CollapsingHeader($"Debug File (ID: {debugData.ID ?? "(null ID)"})")) {
					g.PushID("debug");
					g.Indent(12);
					ImGuiDrawSaveData(debugData);
					g.Unindent(12);
					g.PopID();
				}

				g.Separator();

				AImgui.VSpace(24);

				AImgui.Text("Files on disk (Loadable here for viewing):\n------------------------------------------", ColorsXNA.Goldenrod);

				//g.SameLine();

				if (g.Button("Refresh")) {
					RefreshIDsOnDisk();
				}

				g.SameLine();
				if (g.Button("Load all")) {
					_debugLoadedFromDisk.Clear();

					RefreshIDsOnDisk();

					foreach (SaveFileID id in SaveIDsOnDisk) {
						if (TryLoad(id, out SaveData data)) {
							_debugLoadedFromDisk[id] = data;
						}
					}
				}

				g.SameLine();
				if (g.Button("Dump All")) {
					_debugLoadedFromDisk.Clear(); }


				for (int i = 0; i < SaveIDsOnDisk.Count; i++) {
					g.PushID(i);

					SaveFileID id = SaveIDsOnDisk[i];

					if (g.CollapsingHeader($"{i}: {id} ({(_debugLoadedFromDisk.ContainsKey(id) ? "Loaded" : "Unloaded")})")) {

						g.Indent(12);
						if (!_debugLoadedFromDisk.TryGetValue(id, out SaveData data)) {
							if(g.Button("Try Load")) {
								if(TryLoad(id, out SaveData loaded)) {
									_debugLoadedFromDisk[id] = loaded;
								}
							}
						} else {
							ImGuiDrawSaveData(data);
						}
						g.Unindent(12);
					}

					g.PopID();
				}

				g.BeginGroup();


				g.EndGroup();

				/*if (g.CollapsingHeader("Load Save"))
				{
					g.Indent();

					if (g.Button("Load"))
					{
						UseDebugSave().Forget();
					}

					g.SameLine();
					g.Text("debug");

					g.Spacing();
					g.Separator(); // -----------------------------------------
					g.Spacing();

					if (_cachedSaveNames == null)
					{
						_cachedSaveNames = EnumerateSaves();

					}

					if (g.Button("Refresh"))
					{
						_cachedSaveNames = EnumerateSaves();
					}

					for (var i = 0; i < _cachedSaveNames.Count; i++)
					{
						string saveName = _cachedSaveNames[i];
						g.PushID(i);

						if (g.Button("Load"))
						{
							UseSave(saveName).Forget();
						}

						g.SameLine();
						if (current != null && saveName == current.saveName)
							g.TextColored(Color.green, current.saveName);
						else
							g.Text(saveName);

						g.PopID();
					}

					g.Unindent();
				}

				g.Separator();

				ImGuiDrawSaveData(current);*/
				g.End();
			}
		}

		public static void ImGuiDrawSaveData([CanBeNull] SaveData data)
		{
			//g.Text("Current Save");

			if (data == null)
			{
				g.Text("Data is null.");
				return;
			}

			if (g.Button("Write"))	Write(data);
			g.SameLine();
			if (g.Button("Read"))	Read(data);

			g.Text("Load game from: ");
			g.SameLine();
			if (g.Button("Currently Loaded")) GameController.Live.LoadGameFromSaveData(data);
			g.SameLine();
			if (g.Button("Disk") && data.ID.HasValue) {
				if(TryLoad(data.ID.Value, out SaveData _data))
					GameController.Live.LoadGameFromSaveData(_data);
			}

			if(data.ID.HasValue && data.ID.Value.IsValid()) {
				if (g.Button("Delete"))	{
					g.OpenPopup("_deleteFilePopup");
					_dataDeleting = data;
				}
			}

			if (g.BeginPopupModal("_deleteFilePopup")) {
				g.Text("Are you sure you want to delete this file?");

				if (g.Button("Yes")) {
					DeleteFile(_dataDeleting);
					_dataDeleting = null;
					g.CloseCurrentPopup();
				}

				g.SameLine();

				if(_dataDeleting == null || g.Button("No"))
					g.CloseCurrentPopup();

				g.EndPopup();
			}

			if (g.Button("Reset")) {
				data.Reset();
			}

			g.SameLine();
			if (g.Button("EnsurePlayable")) {
				data.EnsurePlayable();
			}

			g.SameLine();
			if (g.Button("Set Base")) {
				data.Reset();
				data.SetBaseData();
			}

			g.SameLine();
			if (g.Button("Set Maxed")) {
				data.Reset();
				data.SetMaxedData();
			}

			if (g.Button("Prologue Freeport (Party only)")) {
				data.Reset();
				data.SetupPrologueFreeportCombat();
			}

			g.SameLine();
			if (g.Button("Ensure All Demo Limbs")) {
				data.GainPitchBuildLimbs().ForgetWithErrors();
			}


			//g.Text($"Name: {data.saveName}");
			g.Text($"Type: {(current.ID.HasValue ? "savable" : "runtime")}");
			g.Text($"Path: {data.filePath}");

			g.Separator();


			AImgui.DrawObj(data);


			//AnjinGui.EditObj(data);
			//g.InputInt("Money: ", ref data.Money);
		}
	}
}