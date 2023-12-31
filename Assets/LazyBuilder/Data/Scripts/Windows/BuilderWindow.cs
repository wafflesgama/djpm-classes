﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static LazyBuilder.PathFactory;
using static LazyBuilder.ServerManager;

namespace LazyBuilder
{
    public class BuilderWindow : EditorWindow
    {
        private BuilderPreferences preferences;
        private Dictionary<string, Server> serverList;

        private ServerData storedData;
        List<Item>  currentItems;
        List<Item>  currentItemsInPage;

        private int currentPageIndex;

        private string selectedItem;
        private string selectedFile;

        private bool onlyLocalSearch;

        private VisualElement _root;

        private TextField _searchBar;

        //Servers DropDown
        private DropdownField _serversDropdown;
        private VisualElement _serversDropContainer;
        private TextElement _serverDropSelected;
        private VisualElement _serverDropIcon;

        private VisualElement _mainTabContainer;

        //Servers Tab
        private VisualElement _serversTabContainer;
        private Button _serversBackBttn;
        private VisualTreeAsset _serverTemplate;
        private VisualElement _serversListContainer;
        private Button _addServerBttn;
        private Button _removeServerBttn;
        private Button _saveServersBttn;
        private VisualElement _saveServersIcon;
        private VisualElement _selectedListServer;


        //Pagination
        private TextElement _pageIndexMssg;
        private Button _prevPageBttn;
        private Button _nextPageBttn;
        private DropdownField _pageSizeDropdown;

        private TextElement _mainTitle;
        private VisualElement _mainImg;

        private VisualElement _itemTypeIcon;
        private DropdownField _itemTypeDropdown;
        private TextElement _itemTypeSelected;

        private VisualElement _colorPallete;

        private Button _searchBttn;


        //Generation Props
        private TextField _propName;
        private Toggle _propCol;
        private Toggle _propRb;

        private VisualElement _grid;
        private VisualElement _searchBttnIcon;
        private VisualElement _generateBttnIcon;

        private Button _generateBttn;


        private StyleColor defaultButtonColor;
        private StyleColor activeButtonColor;

        //private Material fallbackMat;

        private PreviewRenderUtility previewRenderUtility;
        private Transform previewTransform;
        private bool renderPreview;

        private GameObject previewObj;
        private Mesh previewMesh;
        private Vector3 previewGroundPos;

        private Mesh groundMesh;
        private Material groundMat;


        //Temporary Items
        private List<string> tempFiles;
        private const int tempArraySize = 5;


        //Debug Messager
        private TextElement _debugMssg;


        private Vector3 initPos;
        bool setDist;
        bool isSearchFocused;

        Material[] previewMats;
        List<Color> previewColors;

        private VisualTreeAsset _itemTemplate;

        private Vector2 lastMousePos;
        private bool isRotatingPrev;
        private bool isTranslatingPrev;

        //Messages
        const string LOCALPOOL_MSG = "Local";
        const string MAINPOOL_MSG = "Main";
        const string NEWPOOL_MSG = "Edit servers...";
        const string GROUNDMESHNOT_MSG = "Ground mesh couldn't be fetched";


        #region Unity Functions

        private async void OnEnable()
        {
            InitVariables();
            InitPreferences();

            SetupPreviewUtils();

            SetupStoredItems();
            SetupTempFiles();

            SetupBaseUI();
            SetupBindings();

            _generateBttn.SetEnabled(false);

            SetupCallbacks();
            SetupInputCallbacks();

            SetupPagination();
            SwitchPanel();
            SetupIcons();
            SetupCamera();

            SetupServers();

            RefreshPageIndexMessage();

            //This infinite async loop ensures that the preview's camera is always rendered
            //RepaintCycle();

            //await Task.Delay(1);
            this.Focus();
            _searchBar.Focus();

            LogMessage("Setup finished");
        }


        private void OnDisable()
        {
            previewRenderUtility?.Cleanup();
        }

        private void OnDestroy()
        {
            previewRenderUtility?.Cleanup();
            UpdatePreferences();
        }

        private void OnGUI()
        {
            //if(isTranslatingPrev || isRotatingPrev)
            //    EditorGUIUtility.AddCursorRect(new Rect(20, 20, 140, 40), MouseCursor.Orbit);


            RenderItemPreview();
        }

        #endregion Unity Functions


        private void InitVariables()
        {
            initPos = Vector3.zero;
            defaultButtonColor = new Color(0.345098f, 0.345098f, 0.345098f, 1);
            activeButtonColor = new Color(0.27f, 0.38f, 0.49f);


            MainController.Init(this);
        }


        #region Base UI

        private void SetupBaseUI()
        {
            _root = rootVisualElement;
            // root.styleSheets.Add(Resources.Load<StyleSheet>("qtStyles"));

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(
                PathFactory.BuildUiFilePath(PathFactory.BUILDER_LAYOUT_FILE), typeof(VisualTreeAsset));
            //var quickToolVisualTree = Resources.Load<VisualTreeAsset>("MainLayout");
            quickToolVisualTree.CloneTree(_root);
        }

        private void SetupBindings()
        {
            _mainTitle = (TextElement)_root.Q("TitleText");
            _mainImg = _root.Q("ItemThumbnail");
            _searchBar = (TextField)_root.Q("SearchBar");
            _grid = _root.Q("ItemsColumn1");

            _mainTabContainer = _root.Q("TabContent_Main");
            _serversTabContainer = _root.Q("TabContent_Servers");

            _serversBackBttn = (Button)_root.Q("Back_servers");


            //Servers Dropdown
            _serversDropContainer = _root.Q("PoolsContainer");
            _serversDropdown = (DropdownField)_root.Q("PoolsList");
            _serverDropSelected = (TextElement)_root.Q("PoolSelected");
            _serverDropIcon = _root.Q("ServerDropIcon");

            //Servers Panel
            _serversListContainer = _root.Q("ServersList");
            _addServerBttn = (Button)_root.Q("AddServer");
            _removeServerBttn = (Button)_root.Q("RemoveServer");
            _saveServersBttn = (Button)_root.Q("SaveServersBttn");
            _saveServersIcon = _root.Q("SaveServersIcon");

            //Pagination
            _prevPageBttn = (Button)_root.Q("PrevPageBttn");
            _nextPageBttn = (Button)_root.Q("NextPageBttn");
            _pageIndexMssg = (TextElement)_root.Q("PageIndexMssg");
            _pageSizeDropdown = (DropdownField)_root.Q("PageSizeDropdown");

            _itemTypeIcon = _root.Q("ItemTypeIcon");
            _itemTypeSelected = (TextElement)_root.Q("ItemTypeSelected");
            _itemTypeDropdown = (DropdownField)_root.Q("ItemTypeDropdown");


            //Generation Props
            _propName = (TextField)_root.Q("Prop_Name");
            _propCol = (Toggle)_root.Q("Prop_Col");
            _propRb = (Toggle)_root.Q("Prop_Rb");

            _generateBttn = (Button)_root.Q("GenerateBttn");
            _generateBttnIcon = _root.Q("GenerateBttnIcon");

            _colorPallete = _root.Q("Pallete");

            _searchBttn = (Button)_root.Q("SearchBttn");
            _searchBttnIcon = _root.Q("SearchBttnIcon");

            _debugMssg = (TextElement)_root.Q("Debug");
        }

        private void SetupIcons()
        {
            _searchBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image;
            _generateBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("GameObject Icon").image;
            _itemTypeIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("icon dropdown").image;
            _serverDropIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_icon dropdown@2x").image;
            _saveServersIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_SaveAs").image;
        }

        private void SetupCallbacks()
        {
            //Server Choose
            _serversDropdown.RegisterValueChangedCallback(x => ServerChanged(x.newValue));

            //Servers Edit
            _addServerBttn.clicked += AddServer;
            _removeServerBttn.clicked += RemoveServer;
            _saveServersBttn.clicked += SaveEditServers;
            _serversBackBttn.clicked += () => SwitchPanel(true);

            //Pagination
            _prevPageBttn.clicked += () => ChangePage(false);
            _nextPageBttn.clicked += () => ChangePage(true);
            _pageSizeDropdown.RegisterValueChangedCallback(x => ChangePageSize(x.newValue));
            //Generation Props
            _propCol.RegisterValueChangedCallback(x => preferences.Prop_Col = x.newValue);
            _propRb.RegisterValueChangedCallback(x => preferences.Prop_Rb = x.newValue);

            //Generation
            _generateBttn.clicked += Generate;

            _itemTypeDropdown.RegisterValueChangedCallback(ItemTypeChanged);

            //Search
            _searchBttn.clicked += Search;
            _searchBar.RegisterCallback<FocusInEvent>(OnSearchFocusIn);
            _searchBar.RegisterCallback<FocusOutEvent>(OnSearchFocusOut);
            //_searchBar.RegisterValueChangedCallback(SearchChanged);
        }

        private async void SetupItems(List<Item> items = null)
        {
            _grid.Clear();

            if (items == null)
                items = GetItemList();

            //Pagination
            currentItems = items;
            currentItemsInPage = currentItems.Skip(currentPageIndex * preferences.PageSize).Take(preferences.PageSize).ToList();

            for (int i = 0; i < currentItemsInPage.Count; i++)
            {
                //If window is destroyed - stop setting up items
                if (this == null) return;

                if (_itemTemplate == null)
                    _itemTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(
                        PathFactory.BuildUiFilePath(PathFactory.BUILDER_ITEM_LAYOUT_FILE), typeof(VisualTreeAsset));

                var element = _itemTemplate.CloneTree();
                SetupItem(element, currentItemsInPage[i].Id, i, PathFactory.BuildItemPath(currentItemsInPage[i].Id));
                _grid.Add(element);
                await Task.Delay(1);
            }
            
            RefreshPageButtonsState();
            RefreshPageIndexMessage();
            SelectDefaultItem();
        }

        private async void SetupItem(VisualElement element, string name, int index, string path)
        {
            element.name = name;
            var label = element.Query<Label>().First();
            label.text = name.Capitalize().SeparateCase();

            var button = element.Q<Button>();
            var shadow = element.Q<VisualElement>("Shadow");

            BeginItemAnimation(button, shadow);

            // var buttonIcon = button.Q(className: "quicktool-button-icon");

            Texture2D img = await GetItemThumbnail(name, path);

            var imgHolder = button.Q("Img");
            imgHolder.style.backgroundImage = img;

            var iconOverlay = button.Q("Icon");
            iconOverlay.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Valid").image;

            // Sets a basic tooltip to the button itself.
            button.tooltip = name;

            //Add Callback
            button.clicked += () => ItemSelected(name, img);

            if (!HasStoredItem(name))
            {
                var overlay = button.Q("Overlay");
                overlay.visible = false;
            }
        }


        private void SwitchPanel(bool mainView = true)
        {
            _mainTabContainer.style.display = mainView ? DisplayStyle.Flex : DisplayStyle.None;
            renderPreview = mainView;

            _serversTabContainer.style.display = !mainView ? DisplayStyle.Flex : DisplayStyle.None;

            if (!mainView)
            {
                SetupEditServers();
            }
        }

        #endregion Base UI

        #region Item Render & Preview

        private void SetupPreviewUtils()
        {
            GameObject groundObj = AssetDatabase.LoadAssetAtPath(
                PathFactory.BuildMeshFilePath(PathFactory.MESHES_GROUND_FILE),
                typeof(UnityEngine.Object)) as GameObject;

            if (groundObj == null)
            {
                LogMessage(GROUNDMESHNOT_MSG, 2);
                return;
            }

            groundMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;
            groundMat = AssetDatabase.LoadAssetAtPath<Material>(
                PathFactory.BuildMaterialFilePath(PathFactory.MATERIALS_GROUND_FILE));
        }

        private void SetupCamera()
        {
            //Debug.Log("Camera Setup");
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }

            renderPreview = true;

            previewRenderUtility = new PreviewRenderUtility();
            previewTransform = previewRenderUtility.camera.transform;
            previewRenderUtility.camera.nearClipPlane = .001f;
            previewRenderUtility.camera.farClipPlane = 6000f;
            previewRenderUtility.camera.fieldOfView = 3;

            previewRenderUtility.lights[0].intensity = 1;
            previewRenderUtility.lights[1].intensity = 1;
            previewRenderUtility.ambientColor = Color.white;
            //previewRenderUtility.ambientColor = Color.white;
            //previewRenderUtility.camera.transform.position = new Vector3(0, 10.5f, -18);
            //previewRenderUtility.camera.transform.eulerAngles = new Vector3(30, 0, 0);

            previewTransform.position = new Vector3(0, 10.5f, -18);
            previewRenderUtility.camera.transform.position += (new Vector3(0, -.5835f, 1) * 17);

            //previewRenderUtility.camera.clearFlags = CameraClearFlags.Skybox;
            previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewRenderUtility.camera.backgroundColor = Color.red;

            previewTransform.eulerAngles = new Vector3(30, 0, 0);

            //previewRenderUtility.camera.orthographic = true;
        }


        private void RenderItemPreview()
        {
            if (previewRenderUtility == null || !renderPreview) return;

            Rect rect = _mainImg.worldBound;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);

            //previewRenderUtility.BeginStaticPreview(r);
            previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            //Debug.Log($"OnGUI {tCube} / {tMat}");

            if (setDist)
            {
                setDist = false;
                if (initPos == Vector3.zero)
                    initPos = previewTransform.position;

                previewTransform.position = initPos;
            }


            if (previewMesh != null)
            {
                //Debug.Log("Drawing preview Mesh");

                for (int i = 0; i < previewMats.Length; i++)
                {
                    previewRenderUtility.DrawMesh(previewMesh, Vector3.zero, Quaternion.Euler(0, 0, 0), previewMats[i],
                        i);
                    //previewRenderUtility.camera.transform.RotateAround(previewMesh.bounds.center, Vector3.up, Time.deltaTime);
                }

                previewRenderUtility.DrawMesh(groundMesh, previewGroundPos, Vector3.one * 1000,
                    Quaternion.Euler(90, 0, 0), groundMat, 0, new MaterialPropertyBlock(), null, false);
            }

            previewRenderUtility.camera.Render();
            //var rTexture = previewRenderUtility.EndStaticPreview();
            var image = previewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, image);
        }

        async void RepaintCycle()
        {
            for (int i = 0; i < 9000; i++)
            {
                Repaint();
                await Task.Delay(2);
            }
        }

        private void Zoom(float factor)
        {
            previewRenderUtility.camera.transform.position +=
                previewRenderUtility.camera.transform.forward * 1f * factor;
            Repaint();
        }

        #endregion Item Render & Preview

        #region IO

        private void SetupInputCallbacks()
        {
            _root.RegisterCallback<KeyDownEvent>(OnKeyboardKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseDownEvent>(OnMouseKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseUpEvent>(OnMouseKeyUp, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            _root.RegisterCallback<WheelEvent>(OnMouseWheelDown, TrickleDown.TrickleDown);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isRotatingPrev || isTranslatingPrev)
            {
                var posDif = evt.mousePosition - lastMousePos;
                if (lastMousePos.x != 0)
                {
                    if (isRotatingPrev)
                    {
                        previewRenderUtility.camera.transform.RotateAround(previewMesh.bounds.center,
                            previewRenderUtility.camera.transform.right, posDif.y * 8 * Time.deltaTime);
                        previewRenderUtility.camera.transform.RotateAround(previewMesh.bounds.center, Vector3.up,
                            posDif.x * 8 * Time.deltaTime);
                        Repaint();
                    }

                    if (isTranslatingPrev)
                    {
                        previewRenderUtility.camera.transform.position -= previewRenderUtility.camera.transform.right *
                                                                          posDif.x * 0.07f * Time.deltaTime;
                        previewRenderUtility.camera.transform.position += previewRenderUtility.camera.transform.up *
                                                                          posDif.y * 0.07f * Time.deltaTime;
                        Repaint();
                    }
                }

                lastMousePos = evt.mousePosition;
            }
        }

        private void OnKeyboardKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
                this.Close();

            else if (e.keyCode == KeyCode.Return && isSearchFocused)
            {
                Search();
                _searchBar.Focus();
            }

            else if ((e.keyCode == KeyCode.Return) && selectedFile != "")
            {
                Generate();
            }
        }

        private void OnMouseKeyDown(MouseDownEvent e)
        {
            if ((e.button == 0 || e.button == 2) &&
                CheckIfMouseOverSreen(_mainImg.worldBound, true, new Vector2(0, 20)))
            {
                if (e.button == 0)
                    isRotatingPrev = true;

                if (e.button == 2)
                    isTranslatingPrev = true;

                lastMousePos = Vector2.zero;
            }
        }

        private void OnMouseKeyUp(MouseUpEvent e)
        {
            if (e.button == 0 && isRotatingPrev)
                isRotatingPrev = false;

            if (e.button == 2 && isTranslatingPrev)
                isTranslatingPrev = false;
        }

        private void OnMouseWheelDown(WheelEvent e)
        {
            if (e.delta == Vector3.zero) return;


            if (CheckIfMouseOverSreen(_mainImg.worldBound, true, new Vector2(0, 20)))
            {
                Zoom(e.delta.y);
            }
        }

        private static bool CheckIfMouseOverSreen(Rect r, bool relativePos = true, Vector2? offset = null)
        {
            Vector2 mousePos = relativePos
                ? Event.current.mousePosition
                : GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

            var xMin = r.xMin + (offset.HasValue ? offset.Value.x : 0);
            var xMax = r.xMax + (offset.HasValue ? offset.Value.x : 0);
            var yMin = r.yMin + (offset.HasValue ? offset.Value.y : 0);
            var yMax = r.yMax + (offset.HasValue ? offset.Value.y : 0);

            var condition = mousePos.x >= xMin && mousePos.x <= xMax &&
                            mousePos.y >= yMin && mousePos.y <= yMax;

            return condition;
        }

        #endregion IO

        #region Items Info

        private List<Item> GetItemList() => onlyLocalSearch ? storedData.Items : ServerManager.data.Items;

        private async Task<string> GetItem(string itemId, string itemTypeId)
        {
            var filename = $"{itemId}_{itemTypeId}";
            selectedFile = $"{filename}.{PathFactory.MESH_TYPE}";

            bool storedResult = HasStoredItem(itemId, itemTypeId);
            bool tempResult = HasTempFile(selectedFile);

            string path;

            if (storedResult)
            {
                path = $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{selectedFile}";
            }
            //If Temp file does not exist
            else
            {
                if (!tempResult)
                {
                    LogMessage("Fetching item name" + filename);

                    //Fetch File from server and add to Temp list
                    await ServerManager.server.GetRawFile(PathFactory.BuildItemPath(selectedItem),
                        PathFactory.TEMP_ITEMS_PATH, filename, PathFactory.MESH_TYPE);
                    AssetDatabase.Refresh();
                    AddTempFile(selectedFile);
                }

                path = $"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{selectedFile}";
            }

            return path;
        }

        private async Task<Texture2D> GetItemThumbnail(string itemId, string path)
        {
            //If online search - fetch from server
            if (!onlyLocalSearch)
                return await ServerManager.server.GetImage(path, null, PathFactory.THUMBNAIL_FILE);

            //Else - fetch from local thumbnails folder


            var localPath =
                $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_THUMB_PATH}\\{itemId}.{PathFactory.THUMBNAIL_TYPE}";
            localPath = localPath.AbsoluteFormat();

            if (!File.Exists(localPath)) return null;

            byte[] imageBytes = await File.ReadAllBytesAsync(localPath);

            Texture2D image = new Texture2D(2, 2);
            image.LoadImage(imageBytes);

            return image;
        }

        #endregion Items Info

        #region Stored Items

        private bool HasStoredItem(string itemId, string itemTypeId = null)
        {
            if (itemTypeId == null)
                return storedData.Items.Where(x => x.Id == itemId).Any();

            return storedData.Items.Where(x => x.Id == itemId && x.TypeIds.Contains(itemTypeId)).Any();
        }

        private Item GetStoredItem(string itemId)
        {
            return storedData.Items.Where(x => x.Id == itemId).FirstOrDefault();
        }

        private void SetupStoredItems()
        {
            storedData = new ServerData();
            storedData.Items = new List<Item>();

            string tmpPath = $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_ITEMS_PATH}";

            var files = Utils.GetFiles(tmpPath, true);
            foreach (var file in files)
            {
                //Format: Id_TypeId.fbx
                var fileSplit = file.Split('_');

                if (!HasStoredItem(fileSplit[0]))
                    storedData.Items.Add(new Item() { Id = fileSplit[0] });

                var item = GetStoredItem(fileSplit[0]);

                if (item.TypeIds == null)
                    item.TypeIds = new List<string>();

                //Add TypeId
                item.TypeIds.Add(fileSplit[1].Substring(0, fileSplit[1].IndexOf('.')));
            }
        }

        #endregion Stored Items

        #region Temporary Files Buffer

        private void SetupTempFiles()
        {
            string tmpPath = $"{PathFactory.absoluteToolPath}\\{PathFactory.TEMP_ITEMS_PATH}";
            string[] allFiles = Utils.GetFiles(tmpPath, true);


            if (tempFiles == null)
                tempFiles = new List<string>();
            tempFiles.Clear();

            //Add items to Temp files list at the maximum size of 'tempArraySize'
            int minArrSize = Mathf.Min(tempArraySize, allFiles.Length);
            for (int i = 0; i < minArrSize; i++)
                tempFiles.Add(allFiles[i]);
        }

        private bool HasTempFile(string id)
        {
            bool hasFile = tempFiles.Contains(id);
            //int index = hasFile ? tempItems.IndexOf(id) : -1;

            return hasFile;
        }

        private void AddTempFile(string id)
        {
            // If exceeds the buffer limit - delete the 1st element
            if (tempFiles.Count >= tempArraySize)
                RemoveTempFile(tempFiles[0]);

            tempFiles.Add(id);
        }

        private void RemoveTempFile(string id)
        {
            var filePath = $"{PathFactory.absoluteToolPath}\\{PathFactory.TEMP_ITEMS_PATH}\\{id}";
            filePath = filePath.AbsoluteFormat();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                File.Delete(filePath + ".meta");
            }

            tempFiles.Remove(id);
        }

        private void StoreTempFile(string id)
        {
            var dir = $"{Application.dataPath}/{PathFactory.STORED_ITEMS_PATH}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.MoveAsset($"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{id}",
                $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{id}");

            RemoveTempFile(id);
        }

        #endregion Temporary Items Buffer

        #region Items Search

        private void Search()
        {
            List<Item> items = GetItemList();
            ResetPage();

            var searchVal = _searchBar.value;

            if (String.IsNullOrWhiteSpace(searchVal))
                SetupItems();
            else
            {
                var searchedList = FuzzySearch(items, searchVal);
                SetupItems(searchedList);
            }
        }

        private List<Item> FuzzySearch(List<Item> source, string key)
        {
            return source.Where(x => FuzzyAllMatch(x, key) > 1).OrderByDescending(x => FuzzyIdMatch(x, key))
                .ThenByDescending(x => FuzzyTagMatch(x, key)).ToList();
        }

        private int FuzzyAllMatch(Item source, string key)
        {
            int maxMatches = FuzzyStringMatch(source.Id, key);
            return Mathf.Max(maxMatches, FuzzyTagMatch(source, key));
        }

        private int FuzzyIdMatch(Item source, string key) => FuzzyStringMatch(source.Id, key);

        private int FuzzyTagMatch(Item source, string key)
        {
            int maxMatches = 0;
            foreach (var tag in source.Tags)
                maxMatches = Mathf.Max(maxMatches, FuzzyStringMatch(tag, key));
            return maxMatches;
        }

        private int FuzzyStringMatch(string source, string key)
        {
            int maxMatches = 0, currentMatches = 0;

            var charactersBase = source.ToCharArray();
            var charactersKey = key.ToCharArray();
            for (int i = 0; i < charactersBase.Length; i++)
            {
                if (currentMatches < charactersKey.Length &&
                    charactersKey[currentMatches].Upper() == charactersBase[i].Upper())
                    currentMatches++;
                else if (currentMatches > 0)
                {
                    maxMatches = currentMatches > maxMatches ? currentMatches : maxMatches;
                    currentMatches = 0;
                }
            }

            maxMatches = currentMatches > maxMatches ? currentMatches : maxMatches;
            return maxMatches;
        }

        private void OnSearchFocusIn(FocusInEvent focus)
        {
            isSearchFocused = true;
            _serversDropContainer.style.backgroundColor = activeButtonColor;
            _searchBttn.style.backgroundColor = activeButtonColor;
        }

        private void OnSearchFocusOut(FocusOutEvent focus)
        {
            isSearchFocused = false;
            _serversDropContainer.style.backgroundColor = defaultButtonColor;
            _searchBttn.style.backgroundColor = defaultButtonColor;
        }

        #endregion Items Search

        #region Animation

        private async void BeginItemAnimation(Button button, VisualElement shadow)
        {
            button.style.opacity = 0;
            shadow.style.opacity = 0;

            await Task.Delay(50);
            button.style.opacity = 1;
            shadow.style.opacity = 1;
        }

        #endregion Animation

        #region Object Generation

        private async void Generate()
        {
            StoreTempFile(selectedFile);
            var gObj = AssetDatabase.LoadAssetAtPath(
                $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{selectedFile}",
                typeof(UnityEngine.Object)) as GameObject;

            if (gObj == null) return;
            var newObj = GameObject.Instantiate(gObj);


            newObj.name = _propName.value;

            if (_propCol.value)
            {
                var meshCollider = newObj.AddComponent<MeshCollider>();
                meshCollider.convex = _propRb.value;
            }

            if (_propRb.value)
                newObj.AddComponent<Rigidbody>();

            Selection.activeGameObject = newObj;

            PlaceObjectInView(newObj);


            //Save Thumbnail of Image if not already saved
            var localPath =
                $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_THUMB_PATH}\\{selectedItem}.{PathFactory.THUMBNAIL_TYPE}";
            if (!File.Exists(localPath))
                await ServerManager.server.GetImage(PathFactory.BuildItemPath(selectedItem), localPath,
                    PathFactory.THUMBNAIL_FILE);

            Close();
        }

        private void PlaceObjectInView(GameObject gObject)
        {
            var gMesh = gObject.GetComponent<MeshRenderer>();
            //test.position=  sceneWindow.camera.ScreenToWorldPoint(mPos)+ sceneWindow.camera.transform.forward*2;
            var hits = Physics.RaycastAll(MainController.sceneWindow.camera.transform.position,
                MainController.sceneWindow.camera.transform.forward);
            Vector3? position = null, upVector = null;
            foreach (var hit in hits)
            {
                if (hit.transform.GetInstanceID() == gObject.transform.GetInstanceID()) continue;
                //position = hit.point;
                position = hit.point + new Vector3(0, gMesh.bounds.extents.y, 0) - gMesh.localBounds.center;
                upVector = hit.normal;
                break;
            }

            if (!position.HasValue)
                position = MainController.sceneWindow.camera.transform.forward * 1.5f;

            if (!upVector.HasValue)
                upVector = Vector3.up;

            gObject.transform.position = position.Value;
            //gObject.transform.position = Vector3.zero;
            //gObject.transform.position = sceneWindow.camera.transform.position;
            //gObject.transform.localPosition -= gMesh.localBounds.center;
            gObject.transform.up = upVector.Value;
        }

        private void CreatePrimitive(string primitiveTypeName)
        {
            var pt = (PrimitiveType)Enum.Parse
                (typeof(PrimitiveType), primitiveTypeName, true);
            var go = ObjectFactory.CreatePrimitive(pt);
            go.transform.position = Vector3.zero;
        }

        #endregion Object Generation

        #region Colour Picker

        private VisualElement CreateColorPicker(Color color)
        {
            var field = new ColorField();
            //field.hdr = true;
            field.style.width = 60;
            field.value = color;
            field.RegisterValueChangedCallback(ColorChanged);

            var container = new VisualElement();
            container.Add(field);
            container.style.width = 20;
            container.style.height = 14;
            //container.style.marginRight = 5;
            container.style.overflow = Overflow.Hidden;

            var borderRadius = 10;
            StyleScale s = new Scale(new Vector3(1, 1.2f, 1));
            container.style.scale = s;
            container.style.borderBottomLeftRadius = borderRadius;
            container.style.borderBottomRightRadius = borderRadius;
            container.style.borderTopLeftRadius = borderRadius;
            container.style.borderTopRightRadius = borderRadius;
            return container;
        }

        private void ColorChanged(ChangeEvent<Color> value)
        {
            var index = previewColors.IndexOf(value.previousValue);

            if (index == -1) return;
            previewColors[index] = value.newValue;
            previewMats[index].color = value.newValue;
        }

        #endregion Colour Picker


        #region Pagination

        private void SetupPagination()
        {
            currentPageIndex = 0;

            string prefValue = preferences.PageSize == 0 ? "20" : preferences.PageSize.ToString();

            List<string> pageSizes = new List<string>();
            pageSizes.Add("20");
            pageSizes.Add("50");
            pageSizes.Add("100");
            pageSizes.Add("200");
            if (!pageSizes.Contains(prefValue))
                pageSizes.Add(prefValue);

            _pageSizeDropdown.choices = pageSizes;
            _pageSizeDropdown.value = prefValue;
        }

        private void ChangePage(bool next)
        {
            currentPageIndex += next ? 1 : -1;
            SetupItems();
        }

        private void ResetPage()
        {
            currentPageIndex = 0;
            RefreshPageButtonsState();
        }

        private void ChangePageSize(string newSize)
        {
            int newSizeValue = int.Parse(newSize);
            preferences.PageSize = newSizeValue;

            currentPageIndex = 0;

            RefreshPageButtonsState();
            SetupItems();
            RefreshPageIndexMessage();
            UpdatePreferences();
        }

        private void RefreshPageButtonsState()
        {
            var totalItems = currentItems.Count;
            _prevPageBttn.SetEnabled(currentPageIndex - 1 >= 0);
            _nextPageBttn.SetEnabled((currentPageIndex + 1) * preferences.PageSize < totalItems);
        }

        private void RefreshPageIndexMessage()
        {
            int totalResults = currentItems.Count;
            int initIndex = currentPageIndex * preferences.PageSize;
            int lastIndex = Mathf.Min(initIndex + preferences.PageSize, totalResults);
            _pageIndexMssg.text = $"{initIndex}-{lastIndex} of {totalResults} Results";
        }

        #endregion Pagination

        #region Item & Type Selected

        private void ItemSelected(string itemId, Texture2D icon = null, bool manualTypeSelect = false)
        {
            if (selectedItem == itemId) return;

            _generateBttn.SetEnabled(false);
            preferences.LastItem = itemId;


            selectedItem = itemId;
            //lastSessionItem = itemId;
            _mainImg.style.backgroundImage = icon;
            _mainTitle.text = itemId.Capitalize().SeparateCase();

            _propRb.value = preferences.Prop_Rb;
            _propCol.value = preferences.Prop_Col;

            List<string> choices;
            List<Item> items = GetItemList();

            choices = items.Where(x => x.Id == itemId).FirstOrDefault().TypeIds;

            for (int i = 0; i < choices.Count; i++)
            {
                if (HasStoredItem(itemId, choices[i]))
                    choices[i] = $"{choices[i]}\t✓";
            }

            _itemTypeDropdown.choices = choices;

            if (_itemTypeDropdown.choices.Count > 0 && !manualTypeSelect)
                _itemTypeDropdown.value = _itemTypeDropdown.choices[0];


            setDist = true;
        }

        private void ItemTypeChanged(ChangeEvent<string> value) => ItemTypeSelected(value.newValue);

        private async void ItemTypeSelected(string value)
        {
            //If Type contains a symbol - trim it to get the true Id
            if (value.Contains('\t'))
            {
                value = value.Substring(0, value.IndexOf('\t'));
            }

            if (string.IsNullOrEmpty(value)) return;

            //Update Preferences
            preferences.LastItemType = value;
            UpdatePreferences();

            //Reset Mesh Preview
            previewObj = null;
            previewMesh = null;
            previewMats = null;

            _itemTypeSelected.text = value;
            _propName.value = $"{value.Capitalize()} {selectedItem.Capitalize()}";

            string objPath = await GetItem(selectedItem, value);
            previewObj = AssetDatabase.LoadAssetAtPath(objPath, typeof(UnityEngine.Object)) as GameObject;

            if (previewObj == null)
            {
                LogMessage($"Item {value} couldn't be loaded", 2);
                return;
            }


            var meshRend = previewObj.gameObject.GetComponent<MeshRenderer>();
            previewMats = meshRend.sharedMaterials;
            previewColors = new List<Color>();

            //Make all materials not glossy
            foreach (var material in previewMats)
            {
                material.SetColor("_EmissionColor", material.color);
                material.SetFloat("_Glossiness", 0f);
                previewColors.Add(material.color);
            }

            //Add respective color pickers
            _colorPallete.Clear();
            for (int i = 0; i < previewColors.Count; i++)
            {
                var field = CreateColorPicker(previewColors[i]);
                _colorPallete.Add(field);
            }

            previewMesh = previewObj.gameObject.GetComponent<MeshFilter>().sharedMesh;
            previewGroundPos = new Vector3(previewMesh.bounds.center.x, previewMesh.bounds.min.y - 0.01f,
                previewMesh.bounds.center.z);

            var bounds = previewMesh.bounds;
            var objectSizes = bounds.max - bounds.min;
            var objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);

            if (previewRenderUtility == null || previewRenderUtility.camera == null) return;

            // Visible height 1 meter in front
            var cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * previewRenderUtility.camera.fieldOfView);
            // Combined wanted distance from the object (1 is the distance factor)
            var distance = 1 * objectSize / cameraView;
            // Estimated offset from the center to the outside of the object
            distance += 0.5f * objectSize;

            _generateBttn.SetEnabled(true);

            previewRenderUtility.camera.transform.RotateAround(previewMesh.bounds.center, Vector3.up, Time.deltaTime);
            await Task.Delay(3);

            //If camera has been destroyed while waiting - return
            if (previewRenderUtility == null || previewRenderUtility.camera == null) return;

            var targetPosition = bounds.center - distance * previewRenderUtility.camera.transform.forward;
            previewRenderUtility.camera.transform.position = targetPosition;

            //Debug.Log($"Max bounds {maxSize}");
        }


        private void SelectDefaultItem()
        {
            if (preferences.LastItem != null && preferences.LastItemType != null)
            {
                List<Item> items = GetItemList();

                //Manual selection set to 'true' to avoid auto selecting a typeId 
                ItemSelected(preferences.LastItem, null, true);
                ItemTypeSelected(preferences.LastItemType);
            }
        }

        #endregion Item & Type Selected

        #region Manage Servers

        private void SetupServers()
        {
            serverList = new Dictionary<string, Server>();

            serverList.Add(LOCALPOOL_MSG, null);

            //If Server preferences are not empty
            if (preferences.Servers_src != null && preferences.Servers_Id != null)
            {
                //Load Servers from preferences
                for (int i = 0; i < preferences.Servers_Id.Count; i++)
                {
                    var server = ServerManager.CreateServer(preferences.Servers_Type[i], preferences.Servers_src[i],
                        preferences.Servers_branch[i]);
                    serverList.Add(preferences.Servers_Id[i], server);
                }
            }
            else
            {
                ////Add Default server
                var server =
                    ServerManager.CreateServer(ServerType.GIT, Server_Git.defaultRepo, Server_Git.defaultBranch);

                //Add preferences entry
                preferences.Servers_Id = new List<string>() { MAINPOOL_MSG };
                preferences.Servers_Type = new List<ServerType>() { ServerType.GIT };
                preferences.Servers_src = new List<string> { Server_Git.defaultRepo };
                preferences.Servers_branch = new List<string> { Server_Git.defaultBranch };

                //Add server
                serverList.Add(MAINPOOL_MSG, server);
            }

            //If last server not set - set it to the Main server
            if (preferences.LastServer == null || !serverList.ContainsKey(preferences.LastServer))
            {
                preferences.LastServer = MAINPOOL_MSG;
            }

            serverList.Add(NEWPOOL_MSG, null);
            _serversDropdown.choices = serverList.Keys.ToList();

            _serversDropdown.value = preferences.LastServer;

            //Have to manually Trigger Server Change (bug)
            ServerChanged(preferences.LastServer);
        }


        private void SetupEditServers()
        {
            _serversListContainer.Clear();

            foreach (var server in serverList)
            {
                if (server.Value == null) continue;

                ServerType type = ServerType.GIT;
                if (server.Value is Server_Local)
                    type = ServerType.LOCAL;

                CreateServerEntry(type, server.Key, server.Value.GetSrc(), server.Value.GetBranch());
            }
        }

        private void SaveEditServers()
        {
            foreach (var item in _serversListContainer.Children())
            {
                var idField = (TextField)item.Q("Id");
                var typeField = (DropdownField)item.Q("Type");
                var srcField = (TextField)item.Q("Src");
                var branchField = (TextField)item.Q("Branch");


                //If the element with the same Id 
                if (preferences.Servers_Id.Contains(idField.value))
                {
                    var sameId = preferences.Servers_Id.IndexOf(idField.value);

                    //And with same Src - do not add to the list
                    if (preferences.Servers_src[sameId] == srcField.value)
                        continue;

                    //In case not change Src for the item Id
                    preferences.Servers_src[sameId] = srcField.value;
                    continue;
                }

                preferences.Servers_Id.Add(idField.value);
                preferences.Servers_src.Add(srcField.value);
                preferences.Servers_Type.Add(Enum.Parse<ServerType>(typeField.value));

                //If Server is Type GIT - Add branch
                preferences.Servers_branch.Add(
                    typeField.value == ServerType.GIT.ToString() ? branchField.value : null
                );
            }

            UpdatePreferences();
            SetupServers();
        }

        private void AddServer()
        {
            CreateServerEntry(ServerType.GIT, null, null);
        }

        private void CreateServerEntry(ServerType type, string id = null, string src = null, string branch = null)
        {
            if (_serverTemplate == null)
                _serverTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(
                    PathFactory.BuildUiFilePath(PathFactory.BUILDER_SERVER_ITEM_FILE), typeof(VisualTreeAsset));

            var element = _serverTemplate.CloneTree();

            var idField = (TextField)element.Q("Id");
            if (id != null) idField.value = id;
            idField.RegisterCallback<FocusEvent>((x) => { _selectedListServer = element; });

            var branchField = (TextField)element.Q("Branch");
            if (branch != null) branchField.value = branch;

            var typesField = (DropdownField)element.Q("Type");
            typesField.choices = GetServerTypes();
            typesField.RegisterValueChangedCallback(x =>
            {
                branchField.visible = x.newValue == ServerType.LOCAL.ToString();
            });

            var srcField = (TextField)element.Q("Src");
            if (src != null) srcField.value = src;
            srcField.RegisterCallback<FocusEvent>((x) => { _selectedListServer = element; });

            typesField.value = type.ToString();

            _serversListContainer.Add(element);
        }

        private void RemoveServer()
        {
            if (_selectedListServer == null) return;

            _serversListContainer.Remove(_selectedListServer);

            _selectedListServer = null;
        }


        private async void ServerChanged(string newServer)
        {
            if (newServer == NEWPOOL_MSG)
            {
                SwitchPanel(false);
                return;
            }

            Server currentServer = serverList[newServer];
            ServerManager.SetServer(currentServer);

            await ServerManager.FetchServerData();


            SwitchPanel(true);
            _serverDropSelected.text = newServer;


            onlyLocalSearch = newServer == LOCALPOOL_MSG;
            SetupItems();

            preferences.LastServer = newServer;
            UpdatePreferences();
        }

        #endregion Manage Servers

        #region Preferences

        private void InitPreferences()
        {
            preferences = new BuilderPreferences();
            preferences = PreferenceManager.LoadPreference<BuilderPreferences>(PathFactory.BUILDER_PREFS_FILE);

            if (preferences == null)
                preferences = new BuilderPreferences();
        }

        private void UpdatePreferences()
        {
            PreferenceManager.SavePreference(PathFactory.BUILDER_PREFS_FILE, preferences);
        }

        #endregion Preferences

        #region Utils

        /// <summary>
        /// Logs Message to the footer panel, msg fades after some seconds
        /// </summary>
        /// <param name="message">The displayed message</param>
        /// <param name="messageLevel">0-Info,1-Warning, 2-Error</param>
        private async void LogMessage(string message, int messageLevel = 0)
        {
            _debugMssg.style.opacity = 1;
            _debugMssg.text = message;
            Color color = messageLevel switch
            {
                1 => Color.yellow,
                2 => Color.red,
                _ => Color.white
            };
            _debugMssg.style.color = color;


            for (int i = 0; i < 1000; i++)
            {
                await Task.Delay(5);
                if (_debugMssg.text != message) return;

                var subval = i * 0.01f;
                if (subval > 1) break;

                _debugMssg.style.opacity = 1 - subval;
            }

            _debugMssg.text = "";
        }

        #endregion Utils
    }
}