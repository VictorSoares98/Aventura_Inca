using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GeradorColetaveisAutomatico
{
    private const string PastaMateriais = "Assets/_Project/Materials/Coletaveis";
    private const string PastaDados = "Assets/_Project/Data/Itens";
    private const string PastaPrefabs = "Assets/_Project/Prefabs/Coletaveis";
    private const string PastaPrefabsUI = "Assets/_Project/Prefabs/UI";
    private const string PastaIcones = "Assets/_Project/Textures/Icones";

    [MenuItem("Aventura Inca/1. Gerar Coletáveis, Itens e Prefabs (1 Clique)", false, 1)]
    public static void GerarTudo()
    {
        EditorUtility.DisplayProgressBar("Aventura Inca", "Criando pastas...", 0.1f);
        GarantirPastas();

        EditorUtility.DisplayProgressBar("Aventura Inca", "Criando texturas e ícones...", 0.25f);
        Sprite iconeMoeda = CriarIconeMoeda();
        Sprite iconeReliquia = CriarIconeReliquia();
        Sprite iconePocao = CriarIconePocao();

        EditorUtility.DisplayProgressBar("Aventura Inca", "Criando materiais URP...", 0.45f);
        Material matOuro = CriarMaterialURP("Mat_Ouro", new Color(1f, 0.84f, 0f), 0.85f, 0.75f);
        Material matReliquia = CriarMaterialURP("Mat_Reliquia", new Color(0.85f, 0.65f, 0.13f), 0.6f, 0.5f);
        Material matPocao = CriarMaterialURP("Mat_Pocao", new Color(0.95f, 0.1f, 0.15f), 0.1f, 0.85f);
        Material matMadeira = CriarMaterialURP("Mat_Madeira", new Color(0.36f, 0.2f, 0.09f), 0.0f, 0.2f);

        EditorUtility.DisplayProgressBar("Aventura Inca", "Criando dados dos itens (ScriptableObjects)...", 0.65f);
        ItemDados itemMoeda = CriarItemDados("Moeda_Ouro", "Moeda de Ouro", "Moeda inca de ouro puro.", TipoItem.Moeda, true, 999, 0f, iconeMoeda);
        ItemDados itemReliquia = CriarItemDados("Reliquia_Inca", "Relíquia Inca", "Artefato sagrado dos antigos sacerdotes incas.", TipoItem.Reliquia, false, 1, 0f, iconeReliquia);
        ItemDados itemPocao = CriarItemDados("Pocao_Vida", "Poção de Vida", "Elixir natural que recupera 25 pontos de vida.", TipoItem.Consumivel, true, 5, 25f, iconePocao);

        EditorUtility.DisplayProgressBar("Aventura Inca", "Criando prefabs geométricos...", 0.8f);
        GameObject prefabMoeda = CriarPrefabMoeda(itemMoeda, matOuro);
        GameObject prefabReliquia = CriarPrefabReliquia(itemReliquia, matReliquia);
        GameObject prefabPocao = CriarPrefabPocao(itemPocao, matPocao, matMadeira);

        EditorUtility.DisplayProgressBar("Aventura Inca", "Criando prefab de Slot UI...", 0.9f);
        CriarPrefabSlotUI();

        EditorUtility.DisplayProgressBar("Aventura Inca", "Posicionando amostras na cena...", 0.95f);
        ConfigurarJogadorECena(prefabMoeda, prefabReliquia, prefabPocao);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Passo 1 Concluído!", 
            "Todos os assets foram gerados com sucesso:\n\n" +
            "✔ 3 Itens ScriptableObject (Moeda, Relíquia, Poção)\n" +
            "✔ 3 Ícones estilizados gerados\n" +
            "✔ 4 Materiais URP configurados\n" +
            "✔ 3 Prefabs de Coletáveis salvos em Prefabs/Coletaveis/\n" +
            "✔ 1 Prefab de Slot salvo em Prefabs/UI/\n" +
            "✔ 3 Amostras colocadas à frente do jogador na cena!\n\n" +
            "Agora clique no menu: 'Aventura Inca -> 2. Montar HUD e Inventário na Cena' para finalizar a interface!", "OK");
    }

    [MenuItem("Aventura Inca/2. Montar HUD e Inventário na Cena (1 Clique)", false, 2)]
    public static void MontarUICompletaNaCena()
    {
        EditorUtility.DisplayProgressBar("Aventura Inca", "Localizando jogador e canvas...", 0.2f);

        GameObject jogador = GameObject.Find("Ch18_nonPBR");
        if (jogador == null) jogador = GameObject.FindWithTag("Player");

        if (jogador == null)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Aviso", "Jogador não encontrado na cena. Verifique se o objeto do personagem está presente.", "OK");
            return;
        }

        Inventario inventario = jogador.GetComponent<Inventario>();
        if (inventario == null)
        {
            inventario = jogador.AddComponent<Inventario>();
            EditorUtility.SetDirty(jogador);
        }

        // 1. Canvas e EventSystem
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Criar Canvas");
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(esObj, "Criar EventSystem");
        }

        // 2. Objetivo de Relíquias
        ObjetivoReliquias objetivo = Object.FindFirstObjectByType<ObjetivoReliquias>();
        if (objetivo == null)
        {
            GameObject objObjetivo = new GameObject("Objetivo_Fase");
            objetivo = objObjetivo.AddComponent<ObjetivoReliquias>();
            SerializedObject soObj = new SerializedObject(objetivo);
            soObj.FindProperty("inventario").objectReferenceValue = inventario;
            soObj.FindProperty("meta").intValue = 5;
            soObj.ApplyModifiedProperties();
            Undo.RegisterCreatedObjectUndo(objObjetivo, "Criar Objetivo");
        }

        // 3. HUD
        Transform hudTrans = canvas.transform.Find("HUD_Inventario");
        if (hudTrans == null)
        {
            GameObject hudObj = new GameObject("HUD_Inventario", typeof(RectTransform));
            hudObj.transform.SetParent(canvas.transform, false);
            hudTrans = hudObj.transform;
            RectTransform rtHUD = hudObj.GetComponent<RectTransform>();
            rtHUD.anchorMin = Vector2.zero;
            rtHUD.anchorMax = Vector2.one;
            rtHUD.offsetMin = Vector2.zero;
            rtHUD.offsetMax = Vector2.zero;
        }

        // Contador Moedas
        TextMeshProUGUI tmpMoedas = CriarOuObterTextoTMP(hudTrans, "Contador_Moedas", "Moedas: 0", 
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -40f), new Vector2(200f, 40f), 24, Color.yellow, TextAlignmentOptions.Left, new Vector2(0f, 1f));

        // Contador Relíquias
        TextMeshProUGUI tmpReliquias = CriarOuObterTextoTMP(hudTrans, "Contador_Reliquias", "Relíquias: 0/5", 
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -80f), new Vector2(250f, 40f), 24, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Left, new Vector2(0f, 1f));

        // Notificação
        TextMeshProUGUI tmpNotif = CriarOuObterTextoTMP(hudTrans, "Texto_Notificacao", "", 
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(500f, 50f), 26, Color.white, TextAlignmentOptions.Center);

        // Painel Aviso E
        Transform avisoTrans = hudTrans.Find("Painel_Aviso");
        GameObject painelAviso = null;
        TextMeshProUGUI tmpAviso = null;
        if (avisoTrans == null)
        {
            painelAviso = new GameObject("Painel_Aviso", typeof(RectTransform), typeof(Image));
            painelAviso.transform.SetParent(hudTrans, false);
            RectTransform rtAviso = painelAviso.GetComponent<RectTransform>();
            rtAviso.anchorMin = new Vector2(0.5f, 0.15f);
            rtAviso.anchorMax = new Vector2(0.5f, 0.15f);
            rtAviso.sizeDelta = new Vector2(450f, 45f);
            Image imgAviso = painelAviso.GetComponent<Image>();
            imgAviso.color = new Color(0f, 0f, 0f, 0.75f);

            tmpAviso = CriarOuObterTextoTMP(painelAviso.transform, "Texto_Aviso", "Pressione [E] para coletar",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, Color.white, TextAlignmentOptions.Center);
            painelAviso.SetActive(false);
        }
        else
        {
            painelAviso = avisoTrans.gameObject;
            tmpAviso = avisoTrans.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Anexa HUDInventario
        HUDInventario hudComp = canvas.GetComponent<HUDInventario>();
        if (hudComp == null) hudComp = canvas.gameObject.AddComponent<HUDInventario>();

        SerializedObject soHUD = new SerializedObject(hudComp);
        soHUD.FindProperty("inventario").objectReferenceValue = inventario;
        soHUD.FindProperty("objetivo").objectReferenceValue = objetivo;
        soHUD.FindProperty("textoMoedas").objectReferenceValue = tmpMoedas;
        soHUD.FindProperty("textoReliquias").objectReferenceValue = tmpReliquias;
        soHUD.FindProperty("painelAviso").objectReferenceValue = painelAviso;
        soHUD.FindProperty("textoAviso").objectReferenceValue = tmpAviso;
        soHUD.FindProperty("textoNotificacao").objectReferenceValue = tmpNotif;
        soHUD.ApplyModifiedProperties();

        // 4. Painel de Inventário (Grade de Slots)
        Transform painelInvTrans = canvas.transform.Find("Painel_Inventario");
        GameObject painelInvObj;
        Transform gridTrans;
        TextMeshProUGUI txtNomeDetalhe;
        TextMeshProUGUI txtDescDetalhe;

        if (painelInvTrans == null)
        {
            painelInvObj = new GameObject("Painel_Inventario", typeof(RectTransform), typeof(Image));
            painelInvObj.transform.SetParent(canvas.transform, false);
            RectTransform rtP = painelInvObj.GetComponent<RectTransform>();
            rtP.anchorMin = new Vector2(0.5f, 0.5f);
            rtP.anchorMax = new Vector2(0.5f, 0.5f);
            rtP.sizeDelta = new Vector2(560f, 480f);
            Image imgP = painelInvObj.GetComponent<Image>();
            imgP.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

            // Título
            CriarOuObterTextoTMP(painelInvObj.transform, "Titulo", "INVENTÁRIO [I]",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(400f, 40f), 26, Color.yellow, TextAlignmentOptions.Center);

            // Grade
            GameObject gridObj = new GameObject("Grid_Slots", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObj.transform.SetParent(painelInvObj.transform, false);
            RectTransform rtGrid = gridObj.GetComponent<RectTransform>();
            rtGrid.anchorMin = new Vector2(0.05f, 0.25f);
            rtGrid.anchorMax = new Vector2(0.95f, 0.88f);
            rtGrid.offsetMin = Vector2.zero;
            rtGrid.offsetMax = Vector2.zero;

            GridLayoutGroup glg = gridObj.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(80f, 80f);
            glg.spacing = new Vector2(10f, 10f);
            glg.childAlignment = TextAnchor.UpperLeft;
            gridTrans = gridObj.transform;

            // Detalhes do item selecionado
            txtNomeDetalhe = CriarOuObterTextoTMP(painelInvObj.transform, "Texto_NomeItem", "",
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.22f), Vector2.zero, Vector2.zero, 18, Color.yellow, TextAlignmentOptions.Left);

            txtDescDetalhe = CriarOuObterTextoTMP(painelInvObj.transform, "Texto_DescItem", "",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.15f), Vector2.zero, Vector2.zero, 14, Color.white, TextAlignmentOptions.Left);
        }
        else
        {
            painelInvObj = painelInvTrans.gameObject;
            gridTrans = painelInvTrans.Find("Grid_Slots");
            txtNomeDetalhe = painelInvTrans.Find("Texto_NomeItem")?.GetComponent<TextMeshProUGUI>();
            txtDescDetalhe = painelInvTrans.Find("Texto_DescItem")?.GetComponent<TextMeshProUGUI>();
        }

        // Anexa InventarioUI
        InventarioUI invUI = canvas.GetComponent<InventarioUI>();
        if (invUI == null) invUI = canvas.gameObject.AddComponent<InventarioUI>();

        GameObject prefabSlot = AssetDatabase.LoadAssetAtPath<GameObject>($"{PastaPrefabsUI}/Slot_Item.prefab");
        SlotUI slotUIComponent = prefabSlot != null ? prefabSlot.GetComponent<SlotUI>() : null;

        ControleJogador controleJogador = jogador.GetComponent<ControleJogador>();

        SerializedObject soInvUI = new SerializedObject(invUI);
        soInvUI.FindProperty("inventario").objectReferenceValue = inventario;
        soInvUI.FindProperty("painel").objectReferenceValue = painelInvObj;
        soInvUI.FindProperty("contenedorSlots").objectReferenceValue = gridTrans;
        soInvUI.FindProperty("prefabSlot").objectReferenceValue = slotUIComponent;
        soInvUI.FindProperty("textoNome").objectReferenceValue = txtNomeDetalhe;
        soInvUI.FindProperty("textoDescricao").objectReferenceValue = txtDescDetalhe;
        soInvUI.FindProperty("teclaAbrir").intValue = (int)KeyCode.I;

        SerializedProperty propBloqueados = soInvUI.FindProperty("componentesBloqueados");
        propBloqueados.arraySize = 1;
        propBloqueados.GetArrayElementAtIndex(0).objectReferenceValue = controleJogador;

        soInvUI.ApplyModifiedProperties();

        painelInvObj.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("Trabalho Pronto!", 
            "A interface completa do inventário e HUD foi montada na cena!\n\n" +
            "✔ HUD com contadores de Moedas e Relíquias\n" +
            "✔ Aviso de interação 'Pressione [E]'\n" +
            "✔ Notificação de coleta '+1 Moeda'\n" +
            "✔ Painel de Inventário em grade (Tecla [I])\n" +
            "✔ Bloqueio automático de movimento ao abrir o inventário\n\n" +
            "Dê PLAY agora e teste a coleta e o inventário!", "Excelente!");

        Debug.Log("[GeradorColetaveis] Sucesso: HUD e Inventário montados na cena.");
    }

    private static TextMeshProUGUI CriarOuObterTextoTMP(Transform pai, string nome, string textoInicial, 
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, float fontSize, Color cor, TextAlignmentOptions alinhamento, Vector2? pivot = null)
    {
        Transform t = pai.Find(nome);
        GameObject go = t != null ? t.gameObject : new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(pai, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? (anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f));
        rt.anchoredPosition = anchoredPos;
        if (sizeDelta != Vector2.zero) rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = textoInicial;
        tmp.fontSize = fontSize;
        tmp.color = cor;
        tmp.alignment = alinhamento;
        return tmp;
    }

    private static void GarantirPastas()
    {
        CriarDiretorioSeNaoExistir(PastaMateriais);
        CriarDiretorioSeNaoExistir(PastaDados);
        CriarDiretorioSeNaoExistir(PastaPrefabs);
        CriarDiretorioSeNaoExistir(PastaPrefabsUI);
        CriarDiretorioSeNaoExistir(PastaIcones);
    }

    private static void CriarDiretorioSeNaoExistir(string caminho)
    {
        if (!Directory.Exists(caminho))
        {
            Directory.CreateDirectory(caminho);
        }
    }

    private static Material CriarMaterialURP(string nome, Color cor, float metalico, float suavidade)
    {
        string path = $"{PastaMateriais}/{nome}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = cor;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", cor);

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metalico);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", suavidade);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Sprite CriarIconeMoeda()
    {
        return GerarArquivoSprite("Icone_Moeda", 64, 64, (x, y) =>
        {
            float dx = x - 32f;
            float dy = y - 32f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist > 28f) return Color.clear;
            if (dist > 25f) return new Color(0.7f, 0.5f, 0f);
            if (dist < 12f) return new Color(1f, 0.95f, 0.4f);
            return new Color(1f, 0.84f, 0f);
        });
    }

    private static Sprite CriarIconeReliquia()
    {
        return GerarArquivoSprite("Icone_Reliquia", 64, 64, (x, y) =>
        {
            float dx = Mathf.Abs(x - 32f);
            float dy = Mathf.Abs(y - 32f);

            if (dx + dy > 28f) return Color.clear;
            if (dx + dy > 24f) return new Color(0.5f, 0.35f, 0.05f);
            if (dx < 8f && dy < 8f) return new Color(1f, 0.9f, 0.5f);
            return new Color(0.85f, 0.65f, 0.13f);
        });
    }

    private static Sprite CriarIconePocao()
    {
        return GerarArquivoSprite("Icone_Pocao", 64, 64, (x, y) =>
        {
            if (x >= 27 && x <= 37 && y >= 50 && y <= 58)
                return new Color(0.45f, 0.25f, 0.1f);

            if (x >= 29 && x <= 35 && y >= 40 && y < 50)
                return new Color(0.9f, 0.9f, 1f, 0.8f);

            float dx = x - 32f;
            float dy = y - 24f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist <= 18f)
            {
                if (dist > 16f) return new Color(0.7f, 0.1f, 0.1f);
                if (y < 30) return new Color(0.95f, 0.15f, 0.2f);
                return new Color(1f, 0.6f, 0.6f, 0.7f);
            }

            return Color.clear;
        });
    }

    private static Sprite GerarArquivoSprite(string nome, int largura, int altura, System.Func<int, int, Color> pixelFunc)
    {
        string relativePath = $"{PastaIcones}/{nome}.png";
        string fullPath = Path.Combine(Application.dataPath, "_Project/Textures/Icones", $"{nome}.png");

        Texture2D tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                tex.SetPixel(x, y, pixelFunc(x, y));
            }
        }
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        File.WriteAllBytes(fullPath, pngData);
        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
    }

    private static ItemDados CriarItemDados(string id, string nome, string descricao, TipoItem tipo, bool empilhavel, int limite, float valorCura, Sprite icone)
    {
        string path = $"{PastaDados}/{id}.asset";
        ItemDados item = AssetDatabase.LoadAssetAtPath<ItemDados>(path);

        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemDados>();
            AssetDatabase.CreateAsset(item, path);
        }

        SerializedObject so = new SerializedObject(item);
        so.FindProperty("id").stringValue = id;
        so.FindProperty("nomeExibicao").stringValue = nome;
        so.FindProperty("descricao").stringValue = descricao;
        so.FindProperty("tipo").enumValueIndex = (int)tipo;
        so.FindProperty("empilhavel").boolValue = empilhavel;
        so.FindProperty("limitePilha").intValue = limite;
        so.FindProperty("valorCura").floatValue = valorCura;
        so.FindProperty("icone").objectReferenceValue = icone;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(item);
        return item;
    }

    private static GameObject CriarPrefabMoeda(ItemDados item, Material matOuro)
    {
        GameObject moeda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        moeda.name = "Coletavel_Moeda";
        moeda.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        moeda.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Renderer rend = moeda.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = matOuro;

        Collider col = moeda.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        ItemColetavel coletavel = moeda.AddComponent<ItemColetavel>();
        ConfigurarItemColetavel(coletavel, item, ModoColeta.Automatica);

        return SalvarPrefabEDestruir(moeda, $"{PastaPrefabs}/Coletavel_Moeda.prefab");
    }

    private static GameObject CriarPrefabReliquia(ItemDados item, Material matReliquia)
    {
        GameObject raiz = new GameObject("Coletavel_Reliquia");

        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.SetParent(raiz.transform);
        baseObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        baseObj.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
        Object.DestroyImmediate(baseObj.GetComponent<Collider>());
        baseObj.GetComponent<Renderer>().sharedMaterial = matReliquia;

        GameObject corpoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        corpoObj.name = "Corpo";
        corpoObj.transform.SetParent(raiz.transform);
        corpoObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        corpoObj.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
        Object.DestroyImmediate(corpoObj.GetComponent<Collider>());
        corpoObj.GetComponent<Renderer>().sharedMaterial = matReliquia;

        GameObject cabecaObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabecaObj.name = "Cabeca";
        cabecaObj.transform.SetParent(raiz.transform);
        cabecaObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        cabecaObj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        Object.DestroyImmediate(cabecaObj.GetComponent<Collider>());
        cabecaObj.GetComponent<Renderer>().sharedMaterial = matReliquia;

        BoxCollider boxCol = raiz.AddComponent<BoxCollider>();
        boxCol.center = new Vector3(0f, 0.5f, 0f);
        boxCol.size = new Vector3(0.6f, 1.1f, 0.6f);
        boxCol.isTrigger = true;

        ItemColetavel coletavel = raiz.AddComponent<ItemColetavel>();
        ConfigurarItemColetavel(coletavel, item, ModoColeta.Tecla);

        return SalvarPrefabEDestruir(raiz, $"{PastaPrefabs}/Coletavel_Reliquia.prefab");
    }

    private static GameObject CriarPrefabPocao(ItemDados item, Material matPocao, Material matMadeira)
    {
        GameObject raiz = new GameObject("Coletavel_Pocao");

        GameObject bulbo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulbo.name = "Bulbo";
        bulbo.transform.SetParent(raiz.transform);
        bulbo.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        bulbo.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        Object.DestroyImmediate(bulbo.GetComponent<Collider>());
        bulbo.GetComponent<Renderer>().sharedMaterial = matPocao;

        GameObject gargalo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gargalo.name = "Gargalo";
        gargalo.transform.SetParent(raiz.transform);
        gargalo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        gargalo.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        Object.DestroyImmediate(gargalo.GetComponent<Collider>());
        gargalo.GetComponent<Renderer>().sharedMaterial = matPocao;

        GameObject rolha = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rolha.name = "Rolha";
        rolha.transform.SetParent(raiz.transform);
        rolha.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        rolha.transform.localScale = new Vector3(0.14f, 0.05f, 0.14f);
        Object.DestroyImmediate(rolha.GetComponent<Collider>());
        rolha.GetComponent<Renderer>().sharedMaterial = matMadeira;

        CapsuleCollider capCol = raiz.AddComponent<CapsuleCollider>();
        capCol.center = new Vector3(0f, 0.35f, 0f);
        capCol.radius = 0.25f;
        capCol.height = 0.75f;
        capCol.isTrigger = true;

        ItemColetavel coletavel = raiz.AddComponent<ItemColetavel>();
        ConfigurarItemColetavel(coletavel, item, ModoColeta.Tecla);

        return SalvarPrefabEDestruir(raiz, $"{PastaPrefabs}/Coletavel_Pocao.prefab");
    }

    private static void CriarPrefabSlotUI()
    {
        string path = $"{PastaPrefabsUI}/Slot_Item.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;

        GameObject slotObj = new GameObject("Slot_Item", typeof(RectTransform), typeof(Image), typeof(Button), typeof(SlotUI));
        RectTransform rt = slotObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 80f);

        Image imgFundo = slotObj.GetComponent<Image>();
        imgFundo.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);

        Button btn = slotObj.GetComponent<Button>();

        GameObject iconeObj = new GameObject("Icone", typeof(RectTransform), typeof(Image));
        iconeObj.transform.SetParent(slotObj.transform, false);
        RectTransform rtIcone = iconeObj.GetComponent<RectTransform>();
        rtIcone.sizeDelta = new Vector2(56f, 56f);
        Image imgIcone = iconeObj.GetComponent<Image>();
        imgIcone.enabled = false;

        GameObject textoObj = new GameObject("TextoQuantidade", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObj.transform.SetParent(slotObj.transform, false);
        RectTransform rtTexto = textoObj.GetComponent<RectTransform>();
        rtTexto.anchorMin = new Vector2(0.5f, 0f);
        rtTexto.anchorMax = new Vector2(1f, 0.4f);
        rtTexto.offsetMin = Vector2.zero;
        rtTexto.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textoObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.color = Color.white;
        tmp.text = "";

        SlotUI slotUI = slotObj.GetComponent<SlotUI>();
        SerializedObject so = new SerializedObject(slotUI);
        so.FindProperty("icone").objectReferenceValue = imgIcone;
        so.FindProperty("textoQuantidade").objectReferenceValue = tmp;
        so.FindProperty("botao").objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(slotObj, path);
        Object.DestroyImmediate(slotObj);
    }

    [MenuItem("Aventura Inca/3. Elevar Coletáveis Acima do Solo (Corrigir Afundamento)", false, 3)]
    public static void ElevarColetaveisNaCena()
    {
        ItemColetavel[] coletaveis = Object.FindObjectsByType<ItemColetavel>(FindObjectsSortMode.None);
        int cont = 0;
        foreach (ItemColetavel col in coletaveis)
        {
            col.AjustarAlturaAoTerreno();
            EditorUtility.SetDirty(col.gameObject);
            cont++;
        }

        if (cont > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Ajuste Concluído", $"{cont} coletáveis na cena foram elevados e agora flutuam perfeitamente sobre o solo!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Aviso", "Nenhum coletável foi encontrado na cena atual.", "OK");
        }
    }

    [MenuItem("Aventura Inca/4. Espalhar Coletáveis e Criar Inimigo no Mapa (1 Clique)", false, 4)]
    public static void EspalharColetaveisEEstanciarInimigo()
    {
        GameObject jogador = GameObject.Find("Ch18_nonPBR") ?? GameObject.FindWithTag("Player");
        if (jogador == null)
        {
            EditorUtility.DisplayDialog("Aviso", "Jogador não encontrado na cena.", "OK");
            return;
        }

        Vector3 pos = jogador.transform.position;
        Vector3 frente = jogador.transform.forward;
        Vector3 direita = jogador.transform.right;

        GameObject prefabMoeda = AssetDatabase.LoadAssetAtPath<GameObject>($"{PastaPrefabs}/Coletavel_Moeda.prefab");
        GameObject prefabReliquia = AssetDatabase.LoadAssetAtPath<GameObject>($"{PastaPrefabs}/Coletavel_Reliquia.prefab");
        GameObject prefabPocao = AssetDatabase.LoadAssetAtPath<GameObject>($"{PastaPrefabs}/Coletavel_Pocao.prefab");

        if (prefabMoeda == null || prefabReliquia == null || prefabPocao == null)
        {
            EditorUtility.DisplayDialog("Aviso", "Prefabs não encontrados. Execute o passo 1 primeiro.", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("Aventura Inca", "Espalhando moedas pela trilha...", 0.2f);
        for (int i = 1; i <= 10; i++)
        {
            float dist = i * 4.5f;
            float curva = Mathf.Sin(i * 0.7f) * 2.2f;
            Vector3 spawnPos = pos + frente * dist + direita * curva;
            InstanciarAmostra(prefabMoeda, spawnPos);
        }

        EditorUtility.DisplayProgressBar("Aventura Inca", "Espalhando as 5 relíquias incas...", 0.5f);
        Vector3[] offsetsReliquias = new Vector3[]
        {
            frente * 12f + direita * 3.5f,
            frente * 22f - direita * 3.5f,
            frente * 32f + direita * 2.0f,
            frente * 42f - direita * 2.5f,
            frente * 52f
        };
        for (int i = 0; i < offsetsReliquias.Length; i++)
        {
            InstanciarAmostra(prefabReliquia, pos + offsetsReliquias[i]);
        }

        EditorUtility.DisplayProgressBar("Aventura Inca", "Distribuindo poções de cura...", 0.75f);
        InstanciarAmostra(prefabPocao, pos + frente * 18f - direita * 1.5f);
        InstanciarAmostra(prefabPocao, pos + frente * 36f + direita * 1.5f);
        InstanciarAmostra(prefabPocao, pos + frente * 48f - direita * 1.5f);

        EditorUtility.DisplayProgressBar("Aventura Inca", "Invocando inimigo letal...", 0.9f);
        CriarInimigoLetalNaCena(pos + frente * 12f, jogador);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("Missão Concluída!", 
            "Foram adicionados à fase com sucesso:\n\n" +
            "✔ 10 Moedas de Ouro (formando uma trilha)\n" +
            "✔ 5 Relíquias Incas (meta exata para vencer o objetivo!)\n" +
            "✔ 3 Poções de Vida distribuídas pelo trajeto\n" +
            "✔ 1 Inimigo Letal posicionado 12m à sua frente (Mata ao encostar!)\n\n" +
            "Todos os itens foram automaticamente ajustados para flutuar acima do solo!\n" +
            "Dê PLAY para testar!", "Sensacional!");

        Debug.Log("[GeradorColetaveis] Sucesso: Coletáveis e Inimigo Letal espalhados pelo mapa.");
    }

    [MenuItem("Aventura Inca/5. Posicionar Inimigo a Distância Equilibrada (20m)", false, 5)]
    public static void MoverInimigoParaPertoDoPlayer()
    {
        GameObject jogador = GameObject.Find("Ch18_nonPBR") ?? GameObject.FindWithTag("Player");
        if (jogador == null)
        {
            EditorUtility.DisplayDialog("Aviso", "Jogador não encontrado na cena.", "OK");
            return;
        }

        Vector3 posAlvo = jogador.transform.position + jogador.transform.forward * 20f;
        GameObject inimigo = CriarInimigoLetalNaCena(posAlvo, jogador);

        Selection.activeGameObject = inimigo;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Inimigo Pronto!", 
            "O Inimigo Letal (Besta Sci-Fi) foi colocado a 20 metros à frente do seu personagem na pista!\n\n" +
            "Ele possui 3.5 segundos de espera inicial e preparação do ataque, permitindo que você ande e corra livremente.\n" +
            "Dê PLAY para testar!", "OK");
    }

    [MenuItem("Aventura Inca/6. Destravar e Restaurar Movimento do Jogador (1 Clique)", false, 6)]
    public static void DestravarMovimentoJogador()
    {
        GameObject jogador = GameObject.Find("Ch18_nonPBR") ?? GameObject.FindWithTag("Player");
        if (jogador == null)
        {
            EditorUtility.DisplayDialog("Aviso", "Objeto do Jogador ('Ch18_nonPBR') não encontrado.", "OK");
            return;
        }

        ControleJogador controle = jogador.GetComponent<ControleJogador>();
        if (controle != null) controle.enabled = true;

        CharacterController cc = jogador.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        VidaPersonagem vida = jogador.GetComponent<VidaPersonagem>();
        if (vida != null)
        {
            SerializedObject soVida = new SerializedObject(vida);
            soVida.FindProperty("estaMorto").boolValue = false;
            soVida.FindProperty("vidaAtual").floatValue = 100f;
            soVida.ApplyModifiedProperties();
        }

        // Se o inimigo estiver muito perto do jogador (< 15m), recua para 22m
        InimigoPerseguidor inimigo = Object.FindFirstObjectByType<InimigoPerseguidor>();
        if (inimigo != null)
        {
            float dist = Vector3.Distance(jogador.transform.position, inimigo.transform.position);
            if (dist < 15f)
            {
                inimigo.transform.position = jogador.transform.position + jogador.transform.forward * 22f;
                EditorUtility.SetDirty(inimigo.gameObject);
            }
        }

        EditorUtility.SetDirty(jogador);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Movimento Destravado com Sucesso!",
            "✔ ControleJogador: Ativado\n" +
            "✔ CharacterController: Ativado\n" +
            "✔ Vida: 100/100 (Vivo)\n" +
            "✔ Inimigo ajustado para 22m com 3.5s de carência inicial\n" +
            "✔ Tecla [R] disponível no jogo para reviver instantaneamente caso seja atacado!\n\n" +
            "Dê PLAY e ande normalmente com WASD!", "Perfeito!");
    }

    private static GameObject CriarInimigoLetalNaCena(Vector3 posicao, GameObject jogador)
    {
        InimigoPerseguidor existente = Object.FindFirstObjectByType<InimigoPerseguidor>();
        if (existente != null)
        {
            Undo.DestroyObjectImmediate(existente.gameObject);
        }

        GameObject prefabBesta = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin1.prefab");
        GameObject inimigoObj = null;

        if (prefabBesta != null)
        {
            inimigoObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabBesta);
            inimigoObj.name = "Inimigo_Besta_Letal";
        }
        else
        {
            inimigoObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            inimigoObj.name = "Inimigo_Letal";
            inimigoObj.transform.localScale = new Vector3(1.4f, 1.8f, 1.4f);

            Renderer r = inimigoObj.GetComponent<Renderer>();
            if (r != null)
            {
                Material matInimigo = CriarMaterialURP("Mat_Inimigo", new Color(0.8f, 0.05f, 0.05f), 0.2f, 0.6f);
                r.sharedMaterial = matInimigo;
            }
        }

        Vector3 origemRaio = new Vector3(posicao.x, posicao.y + 20f, posicao.z);
        if (Physics.Raycast(origemRaio, Vector3.down, out RaycastHit hit, 50f))
        {
            posicao = hit.point;
        }
        else if (Terrain.activeTerrain != null)
        {
            posicao.y = Terrain.activeTerrain.SampleHeight(posicao) + Terrain.activeTerrain.transform.position.y;
        }

        inimigoObj.transform.position = posicao;
        Vector3 dirParaJogador = (jogador.transform.position - posicao);
        dirParaJogador.y = 0f;
        if (dirParaJogador.sqrMagnitude > 0.001f)
        {
            inimigoObj.transform.rotation = Quaternion.LookRotation(dirParaJogador);
        }

        NavMeshAgent agente = inimigoObj.GetComponent<NavMeshAgent>();
        if (agente == null) agente = inimigoObj.AddComponent<NavMeshAgent>();
        agente.speed = 3.5f;
        agente.stoppingDistance = 1.2f;
        agente.radius = 0.8f;
        agente.height = 2.0f;

        InimigoPerseguidor seguidor = inimigoObj.GetComponent<InimigoPerseguidor>();
        if (seguidor == null) seguidor = inimigoObj.AddComponent<InimigoPerseguidor>();

        Undo.RegisterCreatedObjectUndo(inimigoObj, "Criar Inimigo Letal");
        return inimigoObj;
    }

    private static void ConfigurarJogadorECena(GameObject prefabMoeda, GameObject prefabReliquia, GameObject prefabPocao)
    {
        GameObject jogador = GameObject.Find("Ch18_nonPBR");
        if (jogador == null)
            jogador = GameObject.FindWithTag("Player");

        if (jogador != null)
        {
            if (jogador.GetComponent<Inventario>() == null)
            {
                jogador.AddComponent<Inventario>();
                EditorUtility.SetDirty(jogador);
            }

            Vector3 frente = jogador.transform.forward;
            Vector3 direita = jogador.transform.right;
            Vector3 posJogador = jogador.transform.position;

            InstanciarAmostra(prefabMoeda, posJogador + frente * 3.5f);
            InstanciarAmostra(prefabReliquia, posJogador + frente * 6f + direita * 1.8f);
            InstanciarAmostra(prefabPocao, posJogador + frente * 6f - direita * 1.8f);
        }
    }

    private static void InstanciarAmostra(GameObject prefab, Vector3 posicao)
    {
        if (prefab == null) return;

        Vector3 origemRaio = new Vector3(posicao.x, posicao.y + 15f, posicao.z);
        if (Physics.Raycast(origemRaio, Vector3.down, out RaycastHit hit, 40f))
        {
            posicao = new Vector3(posicao.x, hit.point.y + 0.85f, posicao.z);
        }
        else if (Terrain.activeTerrain != null)
        {
            float y = Terrain.activeTerrain.SampleHeight(posicao) + Terrain.activeTerrain.transform.position.y;
            posicao = new Vector3(posicao.x, y + 0.85f, posicao.z);
        }
        else
        {
            posicao = new Vector3(posicao.x, posicao.y + 0.85f, posicao.z);
        }

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instancia != null)
        {
            instancia.transform.position = posicao;
            Undo.RegisterCreatedObjectUndo(instancia, "Instanciar Coletavel Amostra");
        }
    }

    private static void ConfigurarItemColetavel(ItemColetavel coletavel, ItemDados item, ModoColeta modo)
    {
        SerializedObject so = new SerializedObject(coletavel);
        so.FindProperty("item").objectReferenceValue = item;
        so.FindProperty("quantidade").intValue = 1;
        so.FindProperty("modoColeta").enumValueIndex = (int)modo;
        so.FindProperty("teclaColeta").intValue = (int)KeyCode.E;
        so.FindProperty("tagJogador").stringValue = "Player";
        so.FindProperty("velocidadeGiro").floatValue = 90f;
        so.FindProperty("amplitudeFlutuacao").floatValue = 0.15f;
        so.FindProperty("frequenciaFlutuacao").floatValue = 1.5f;
        so.ApplyModifiedProperties();
    }

    private static GameObject SalvarPrefabEDestruir(GameObject obj, string caminhoPrefab)
    {
        GameObject prefabSalvo = PrefabUtility.SaveAsPrefabAsset(obj, caminhoPrefab);
        Object.DestroyImmediate(obj);
        return prefabSalvo;
    }
}
