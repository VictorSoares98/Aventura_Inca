using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click Editor generator for Stage 4: modular phase prefabs (Portal, BGM, Victory),
/// Cena_MenuPrincipal, Cena_Fase2_Ruinas and Cena_Fase3_Nave cloned onto distinct regions
/// of the real terrain map, plus Build Settings registration (0..3).
/// </summary>
public static class GeradorMenuECenas
{
    private const string PastaPrefabsSistema = "Assets/_Project/Prefabs/Sistema";
    private const string PastaMateriaisSistema = "Assets/_Project/Materials/Sistema";
    private const string PastaPrefabsColetaveis = "Assets/_Project/Prefabs/Coletaveis";
    private const string PastaCenas = "Assets/_Project/Scenes";

    private const string PathPrefabPortal = PastaPrefabsSistema + "/Prefab_PortalProximaFase.prefab";
    private const string PathPrefabMusica = PastaPrefabsSistema + "/Prefab_MusicaFundo.prefab";
    private const string PathPrefabVitoria = PastaPrefabsSistema + "/Prefab_TelaVitoria.prefab";

    private const string PathPrefabJogador = PastaPrefabsSistema + "/Prefab_Jogador.prefab";
    private const string PathPrefabHud = PastaPrefabsSistema + "/Prefab_HUD_Sistema.prefab";
    private const string PathPrefabCamera = PastaPrefabsSistema + "/Prefab_Camera.prefab";

    private const string PathCenaBaseMapa = PastaCenas + "/Cena_Coletaveis_Inventario.unity";
    private const string PathCenaMenu = PastaCenas + "/Cena_MenuPrincipal.unity";
    private const string PathCenaFase1 = PastaCenas + "/Cena_principal.unity";
    private const string PathCenaFase2 = PastaCenas + "/Cena_Fase2_Ruinas.unity";
    private const string PathCenaFase3 = PastaCenas + "/Cena_Fase3_Nave.unity";

    private const string PathMusicaMenu = "Assets/Desert Music Pack/Loops/1. Dunes of Silence.wav";
    private const string PathMusicaFase2 = "Assets/Desert Music Pack/Loops/3. Whispers in the Sand.wav";
    private const string PathMusicaFase3 = "Assets/Desert Music Pack/Loops/4. Canyon Echoes.wav";

    private struct ConfigFaseMapa
    {
        public string CaminhoDestino;
        public bool EhFase3;
        public string CaminhoMusica;
        public string CenaDestinoPortal;
        public bool PortalFinal;
        public Color CorLuz;
        public float IntensidadeLuz;
        public Vector3 RotacaoLuz;
        public Color CorFog;
        public float DensidadeFog;
    }

    [MenuItem("Aventura Inca/11. Gerar Prefabs de Fase (Portal, Musica, Vitoria)", false, 11)]
    public static void GerarPrefabsDeFase()
    {
        GerarPrefabsDeFaseInterno(exibirDialogo: true);
    }

    [MenuItem("Aventura Inca/12. Montar Menu Principal + Fases 2 e 3 (1 Clique)", false, 12)]
    public static void MontarMenuEFases2e3()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        GarantirPastas();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabPortal) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabMusica) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabVitoria) == null)
        {
            GerarPrefabsDeFaseInterno(exibirDialogo: false);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabJogador) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabHud) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabCamera) == null)
        {
            EditorUtility.DisplayDialog(
                "Prefabs de Sistema Ausentes",
                "Não foram encontrados Prefab_Jogador, Prefab_HUD_Sistema ou Prefab_Camera em Assets/_Project/Prefabs/Sistema/.\n\n" +
                "Abra uma cena válida e execute primeiro o menu 'Aventura Inca -> 8. Gerar Prefabs de Sistema'.",
                "OK");
            return;
        }

        int registradas = 0;
        try
        {
            EditorUtility.DisplayProgressBar("Aventura Inca", "Montando Cena_MenuPrincipal...", 0.2f);
            CriarCenaMenuPrincipal();

            EditorUtility.DisplayProgressBar("Aventura Inca", "Clonando mapa real para Cena_Fase2_Ruinas (Entardecer)...", 0.5f);
            CriarCenaFase2Ruinas();

            EditorUtility.DisplayProgressBar("Aventura Inca", "Clonando mapa real para Cena_Fase3_Nave (Crepúsculo)...", 0.8f);
            CriarCenaFase3Nave();

            EditorUtility.DisplayProgressBar("Aventura Inca", "Registrando Build Settings (0..3)...", 0.95f);
            registradas = RegistrarCenasNoBuildSettings(silencioso: true);

            EditorSceneManager.OpenScene(PathCenaMenu, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "Etapa 4 Gerada com Sucesso!",
            $"✔ Cena_MenuPrincipal.unity criada (Build 0)\n" +
            $"✔ Cena_Fase2_Ruinas.unity criada em outra região do mapa real (Entardecer, Build 2)\n" +
            $"✔ Cena_Fase3_Nave.unity criada na terceira região do mapa real (Crepúsculo + Tela de Vitória, Build 3)\n" +
            $"✔ {registradas}/4 cenas registradas no EditorBuildSettings\n\n" +
            "Nota (ADR-04): Cena_principal.unity e Cena_Coletaveis_Inventario.unity permaneceram intactas.\n" +
            "Na integração da Fase 1, basta arrastar Prefab_PortalProximaFase (destino: Cena_Fase2_Ruinas) e Prefab_MusicaFundo.\n\n" +
            "Pressione PLAY na Cena_MenuPrincipal para testar todo o percurso!",
            "Excelente!");
    }

    [MenuItem("Aventura Inca/13. Registrar 4 Cenas no Build Settings (0..3)", false, 13)]
    public static void RegistrarCenasNoBuildSettingsMenu()
    {
        RegistrarCenasNoBuildSettings(silencioso: false);
    }

    private static void GerarPrefabsDeFaseInterno(bool exibirDialogo)
    {
        GarantirPastas();

        CriarPrefabPortal();
        CriarPrefabMusicaFundo();
        CriarPrefabTelaVitoria();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (exibirDialogo)
        {
            EditorUtility.DisplayDialog(
                "Prefabs de Fase Gerados!",
                "Foram criados/atualizados em Assets/_Project/Prefabs/Sistema/:\n\n" +
                "✔ Prefab_PortalProximaFase.prefab\n" +
                "✔ Prefab_MusicaFundo.prefab\n" +
                "✔ Prefab_TelaVitoria.prefab",
                "OK");
        }
    }

    private static void GarantirPastas()
    {
        GarantirPasta(PastaPrefabsSistema);
        GarantirPasta(PastaMateriaisSistema);
        GarantirPasta(PastaCenas);
    }

    private static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho))
            return;

        string[] partes = caminho.Split('/');
        string atual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string proxima = atual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(proxima))
                AssetDatabase.CreateFolder(atual, partes[i]);
            atual = proxima;
        }
    }

    private static Material CriarOuObterMaterialURP(string nomeArquivo, Color corBase, float metallic, float smoothness, Color? corEmissiva = null)
    {
        string path = $"{PastaMateriaisSistema}/{nomeArquivo}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", corBase);
        mat.color = corBase;

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        if (corEmissiva.HasValue && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", corEmissiva.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void CriarPrefabPortal()
    {
        Material matPilar = CriarOuObterMaterialURP("Mat_Portal_PedraInca", new Color(0.72f, 0.56f, 0.25f, 1f), 0.25f, 0.45f);
        Material matEnergia = CriarOuObterMaterialURP("Mat_Portal_Energia", new Color(0.15f, 0.85f, 1f, 0.85f), 0.1f, 0.9f, new Color(0.2f, 1.4f, 1.8f));

        GameObject raiz = new GameObject("Prefab_PortalProximaFase");
        BoxCollider trigger = raiz.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.8f, 0f);
        trigger.size = new Vector3(3.4f, 3.6f, 2.2f);

        PortalProximaFase portal = raiz.AddComponent<PortalProximaFase>();
        SerializedObject soPortal = new SerializedObject(portal);
        AudioClip somReliquia = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/SFX_Coleta_Reliquia.wav");
        if (somReliquia != null)
            soPortal.FindProperty("somPortal").objectReferenceValue = somReliquia;
        soPortal.ApplyModifiedProperties();

        CriarBlocoVisual(raiz.transform, "Pilar_Esquerdo", PrimitiveType.Cube, new Vector3(-1.55f, 1.8f, 0f), new Vector3(0.55f, 3.6f, 0.65f), matPilar);
        CriarBlocoVisual(raiz.transform, "Pilar_Direito", PrimitiveType.Cube, new Vector3(1.55f, 1.8f, 0f), new Vector3(0.55f, 3.6f, 0.65f), matPilar);
        CriarBlocoVisual(raiz.transform, "Arco_Superior", PrimitiveType.Cube, new Vector3(0f, 3.75f, 0f), new Vector3(3.8f, 0.5f, 0.75f), matPilar);
        CriarBlocoVisual(raiz.transform, "Base_Pedestal", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(4.0f, 0.2f, 2.2f), matPilar);
        CriarBlocoVisual(raiz.transform, "Campo_Energia", PrimitiveType.Cube, new Vector3(0f, 1.85f, 0f), new Vector3(2.4f, 3.1f, 0.12f), matEnergia);

        GameObject luzObj = new GameObject("Luz_Portal");
        luzObj.transform.SetParent(raiz.transform, false);
        luzObj.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        Light luz = luzObj.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.3f, 0.9f, 1f);
        luz.intensity = 6f;
        luz.range = 10f;

        PrefabUtility.SaveAsPrefabAsset(raiz, PathPrefabPortal);
        UnityEngine.Object.DestroyImmediate(raiz);
    }

    private static void CriarBlocoVisual(Transform pai, string nome, PrimitiveType tipo, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(tipo);
        obj.name = nome;
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            UnityEngine.Object.DestroyImmediate(col);

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && mat != null)
            rend.sharedMaterial = mat;
    }

    private static void CriarPrefabMusicaFundo()
    {
        GameObject raiz = new GameObject("Prefab_MusicaFundo");
        AudioSource audio = raiz.AddComponent<AudioSource>();
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PathMusicaMenu);
        audio.playOnAwake = true;
        audio.loop = true;
        audio.spatialBlend = 0f;
        audio.volume = 0.35f;

        PrefabUtility.SaveAsPrefabAsset(raiz, PathPrefabMusica);
        UnityEngine.Object.DestroyImmediate(raiz);
    }

    private static void CriarPrefabTelaVitoria()
    {
        GameObject canvasObj = new GameObject("Prefab_TelaVitoria", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TelaVitoriaUI));
        canvasObj.layer = 5;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject painel = CriarPainelUI(canvasObj.transform, "PainelVitoria", new Color(0.05f, 0.04f, 0.03f, 0.88f));
        RectTransform rtPainel = painel.GetComponent<RectTransform>();
        rtPainel.anchorMin = Vector2.zero;
        rtPainel.anchorMax = Vector2.one;
        rtPainel.offsetMin = Vector2.zero;
        rtPainel.offsetMax = Vector2.zero;

        GameObject caixa = CriarPainelUI(painel.transform, "CaixaCentral", new Color(0.14f, 0.1f, 0.06f, 0.96f));
        RectTransform rtCaixa = caixa.GetComponent<RectTransform>();
        rtCaixa.anchorMin = new Vector2(0.5f, 0.5f);
        rtCaixa.anchorMax = new Vector2(0.5f, 0.5f);
        rtCaixa.sizeDelta = new Vector2(720f, 420f);
        rtCaixa.anchoredPosition = Vector2.zero;

        CriarTextoUI(caixa.transform, "Titulo_Vitoria", "VITÓRIA! O TESOURO INCA É SEU!",
            new Vector2(0f, 110f), new Vector2(640f, 80f), 38f, FontStyles.Bold, new Color(1f, 0.84f, 0.25f));

        CriarTextoUI(caixa.transform, "Subtitulo_Vitoria",
            "Todas as fases foram concluídas e os artefatos sagrados foram recuperados.",
            new Vector2(0f, 35f), new Vector2(620f, 60f), 22f, FontStyles.Normal, Color.white);

        TelaVitoriaUI telaVitoria = canvasObj.GetComponent<TelaVitoriaUI>();

        Button btnReplay = CriarBotaoUI(caixa.transform, "Btn_JogarNovamente", "JOGAR NOVAMENTE",
            new Vector2(0f, -55f), new Vector2(360f, 62f), new Color(0.76f, 0.54f, 0.14f));
        UnityEventTools.AddVoidPersistentListener(btnReplay.onClick, telaVitoria.JogarNovamente);

        Button btnMenu = CriarBotaoUI(caixa.transform, "Btn_VoltarMenu", "VOLTAR AO MENU PRINCIPAL",
            new Vector2(0f, -135f), new Vector2(360f, 62f), new Color(0.24f, 0.24f, 0.28f));
        UnityEventTools.AddVoidPersistentListener(btnMenu.onClick, telaVitoria.VoltarAoMenu);

        SerializedObject so = new SerializedObject(telaVitoria);
        so.FindProperty("painelVitoria").objectReferenceValue = painel;
        so.ApplyModifiedProperties();

        painel.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(canvasObj, PathPrefabVitoria);
        UnityEngine.Object.DestroyImmediate(canvasObj);
    }

    private static void CriarCenaMenuPrincipal()
    {
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 1f, -10f);
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.07f, 0.05f, 1f);

        GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject bgmObj = InstanciarPrefabSeExistir(PathPrefabMusica, Vector3.zero, "MusicaFundo_Menu");
        DefinirClipAudio(bgmObj, PathMusicaMenu);

        GameObject canvasObj = new GameObject("Canvas_MenuPrincipal", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MenuPrincipalController));
        canvasObj.layer = 5;
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject fundo = CriarPainelUI(canvasObj.transform, "Fundo_Menu", new Color(0.08f, 0.06f, 0.04f, 1f));
        RectTransform rtFundo = fundo.GetComponent<RectTransform>();
        rtFundo.anchorMin = Vector2.zero;
        rtFundo.anchorMax = Vector2.one;
        rtFundo.offsetMin = Vector2.zero;
        rtFundo.offsetMax = Vector2.zero;

        CriarTextoUI(fundo.transform, "Titulo_Jogo", "O TESOURO PERDIDO DAS TERRAS SECAS",
            new Vector2(0f, 270f), new Vector2(1200f, 90f), 48f, FontStyles.Bold, new Color(1f, 0.82f, 0.24f));

        CriarTextoUI(fundo.transform, "Subtitulo_Jogo", "Versão Arqueólogo Metamorfo",
            new Vector2(0f, 205f), new Vector2(800f, 45f), 24f, FontStyles.Italic, new Color(0.88f, 0.82f, 0.7f));

        MenuPrincipalController controller = canvasObj.GetComponent<MenuPrincipalController>();

        GameObject painelPrincipal = CriarPainelUI(fundo.transform, "PainelPrincipal", new Color(0.14f, 0.1f, 0.06f, 0.9f));
        RectTransform rtPrincipal = painelPrincipal.GetComponent<RectTransform>();
        rtPrincipal.anchorMin = new Vector2(0.5f, 0.5f);
        rtPrincipal.anchorMax = new Vector2(0.5f, 0.5f);
        rtPrincipal.sizeDelta = new Vector2(460f, 340f);
        rtPrincipal.anchoredPosition = new Vector2(0f, -20f);

        Button btnJogar = CriarBotaoUI(painelPrincipal.transform, "Btn_Jogar", "JOGAR",
            new Vector2(0f, 90f), new Vector2(360f, 68f), new Color(0.78f, 0.54f, 0.12f));
        UnityEventTools.AddVoidPersistentListener(btnJogar.onClick, controller.Jogar);

        Button btnFases = CriarBotaoUI(painelPrincipal.transform, "Btn_SelecionarFase", "SELECIONAR FASE",
            new Vector2(0f, 0f), new Vector2(360f, 64f), new Color(0.24f, 0.34f, 0.44f));
        UnityEventTools.AddVoidPersistentListener(btnFases.onClick, controller.AbrirSelecaoFases);

        Button btnSair = CriarBotaoUI(painelPrincipal.transform, "Btn_Sair", "SAIR",
            new Vector2(0f, -90f), new Vector2(360f, 64f), new Color(0.48f, 0.16f, 0.16f));
        UnityEventTools.AddVoidPersistentListener(btnSair.onClick, controller.SairDoJogo);

        GameObject painelFases = CriarPainelUI(fundo.transform, "PainelSelecaoFases", new Color(0.14f, 0.1f, 0.06f, 0.94f));
        RectTransform rtFases = painelFases.GetComponent<RectTransform>();
        rtFases.anchorMin = new Vector2(0.5f, 0.5f);
        rtFases.anchorMax = new Vector2(0.5f, 0.5f);
        rtFases.sizeDelta = new Vector2(540f, 400f);
        rtFases.anchoredPosition = new Vector2(0f, -20f);

        CriarTextoUI(painelFases.transform, "Titulo_Fases", "SELECIONE A FASE",
            new Vector2(0f, 145f), new Vector2(480f, 50f), 28f, FontStyles.Bold, new Color(1f, 0.82f, 0.24f));

        Button btnF1 = CriarBotaoUI(painelFases.transform, "Btn_Fase1", "FASE 1 — TERRAS SECAS",
            new Vector2(0f, 70f), new Vector2(440f, 58f), new Color(0.68f, 0.48f, 0.14f));
        UnityEventTools.AddVoidPersistentListener(btnF1.onClick, controller.CarregarFase1);

        Button btnF2 = CriarBotaoUI(painelFases.transform, "Btn_Fase2", "FASE 2 — RUÍNAS ANTIGAS",
            new Vector2(0f, 0f), new Vector2(440f, 58f), new Color(0.68f, 0.48f, 0.14f));
        UnityEventTools.AddVoidPersistentListener(btnF2.onClick, controller.CarregarFase2);

        Button btnF3 = CriarBotaoUI(painelFases.transform, "Btn_Fase3", "FASE 3 — DESTROÇOS DA NAVE",
            new Vector2(0f, -70f), new Vector2(440f, 58f), new Color(0.68f, 0.48f, 0.14f));
        UnityEventTools.AddVoidPersistentListener(btnF3.onClick, controller.CarregarFase3);

        Button btnVoltar = CriarBotaoUI(painelFases.transform, "Btn_Voltar", "VOLTAR",
            new Vector2(0f, -145f), new Vector2(260f, 52f), new Color(0.25f, 0.25f, 0.28f));
        UnityEventTools.AddVoidPersistentListener(btnVoltar.onClick, controller.FecharSelecaoFases);

        painelFases.SetActive(false);

        TextMeshProUGUI textoStatus = CriarTextoUI(fundo.transform, "TextoStatus", string.Empty,
            new Vector2(0f, -260f), new Vector2(1000f, 45f), 20f, FontStyles.Normal, new Color(1f, 0.45f, 0.45f));

        SerializedObject soCtrl = new SerializedObject(controller);
        soCtrl.FindProperty("painelPrincipal").objectReferenceValue = painelPrincipal;
        soCtrl.FindProperty("painelSelecaoFases").objectReferenceValue = painelFases;
        soCtrl.FindProperty("textoStatus").objectReferenceValue = textoStatus;
        soCtrl.ApplyModifiedProperties();

        Dictionary<string, Transform> raiz = CriarEstruturaRaiz(cena);
        ParentearSeNaoNulo(camObj, raiz["CAMERA"]);
        ParentearSeNaoNulo(canvasObj, raiz["HUD"]);
        ParentearSeNaoNulo(esObj, raiz["SISTEMAS"]);
        ParentearSeNaoNulo(bgmObj, raiz["AUDIO"]);

        EditorSceneManager.SaveScene(cena, PathCenaMenu);
    }

    private static void CriarCenaFase2Ruinas()
    {
        ClonarEConfigurarFaseNoMapaReal(new ConfigFaseMapa
        {
            CaminhoDestino = PathCenaFase2,
            EhFase3 = false,
            CaminhoMusica = PathMusicaFase2,
            CenaDestinoPortal = "Cena_Fase3_Nave",
            PortalFinal = false,
            CorLuz = new Color(1f, 0.706f, 0.42f),
            IntensidadeLuz = 1.15f,
            RotacaoLuz = new Vector3(22f, 135f, 0f),
            CorFog = new Color(0.76f, 0.52f, 0.34f),
            DensidadeFog = 0.0045f
        });
    }

    private static void CriarCenaFase3Nave()
    {
        ClonarEConfigurarFaseNoMapaReal(new ConfigFaseMapa
        {
            CaminhoDestino = PathCenaFase3,
            EhFase3 = true,
            CaminhoMusica = PathMusicaFase3,
            CenaDestinoPortal = string.Empty,
            PortalFinal = true,
            CorLuz = new Color(0.431f, 0.51f, 0.722f),
            IntensidadeLuz = 0.65f,
            RotacaoLuz = new Vector3(18f, -125f, 0f),
            CorFog = new Color(0.18f, 0.22f, 0.32f),
            DensidadeFog = 0.006f
        });
    }

    private static void ClonarEConfigurarFaseNoMapaReal(ConfigFaseMapa config)
    {
        string origemPath = File.Exists(PathCenaBaseMapa) ? PathCenaBaseMapa : PathCenaFase1;
        if (!File.Exists(origemPath))
        {
            Debug.LogError("[GeradorMenuECenas] Nenhuma cena base encontrada para clonar o mapa real.");
            return;
        }

        if (File.Exists(config.CaminhoDestino))
            AssetDatabase.DeleteAsset(config.CaminhoDestino);

        if (!AssetDatabase.CopyAsset(origemPath, config.CaminhoDestino))
        {
            Debug.LogError($"[GeradorMenuECenas] Falha ao clonar '{origemPath}' para '{config.CaminhoDestino}'.");
            return;
        }

        Scene cena = EditorSceneManager.OpenScene(config.CaminhoDestino, OpenSceneMode.Single);

        LimparAtoresGameplayClonados();

        Terrain terreno = Terrain.activeTerrain;
        Vector3 tPos = terreno != null ? terreno.transform.position : Vector3.zero;
        Vector3 tSize = (terreno != null && terreno.terrainData != null) ? terreno.terrainData.size : new Vector3(120f, 10f, 120f);

        GameObject jogadorObj = LocalizarOuInstanciarJogador();
        Vector3 posJogadorAtual = jogadorObj != null ? jogadorObj.transform.position : tPos + tSize * 0.5f;

        float px = tSize.x > 0.01f ? Mathf.Clamp01((posJogadorAtual.x - tPos.x) / tSize.x) : 0.5f;
        float pz = tSize.z > 0.01f ? Mathf.Clamp01((posJogadorAtual.z - tPos.z) / tSize.z) : 0.5f;

        float comp = Mathf.Clamp(Mathf.Min(tSize.x, tSize.z) * 0.35f, 32f, 56f);
        float margem = Mathf.Min(18f, Mathf.Min(tSize.x, tSize.z) * 0.12f);

        Vector3 inicioZonaXZ;
        Vector3 dir;
        Vector3 direita;

        if (!config.EhFase3)
        {
            float nx = px < 0.5f ? 0.72f : 0.28f;
        inicioZonaXZ = new Vector3(tPos.x + tSize.x * nx, 0f, tPos.z + tSize.z * 0.25f);
        dir = Vector3.forward;
        direita = Vector3.right;
    }
        else
        {
float nz = pz < 0.5f ? 0.72f : 0.28f;
inicioZonaXZ = new Vector3(tPos.x + tSize.x * 0.25f, 0f, tPos.z + tSize.z * nz);
dir = Vector3.right;
direita = Vector3.back;
        }

Vector3 inicioNoTerreno = PontoNoTerreno(terreno, inicioZonaXZ, margem, 0.08f);
if (jogadorObj != null)
{
CharacterController cc = jogadorObj.GetComponent<CharacterController>();
bool ccAtivo = cc != null && cc.enabled;
if (ccAtivo) cc.enabled = false;

jogadorObj.transform.SetPositionAndRotation(inicioNoTerreno, Quaternion.LookRotation(dir));

if (ccAtivo) cc.enabled = true;
        }

Dictionary<string, Transform> raiz = CriarEstruturaRaiz(cena);
if (jogadorObj != null && jogadorObj.transform.parent == null)
    jogadorObj.transform.SetParent(raiz["PLAYER"], true);

GarantirHudECamera(raiz, config.EhFase3);
ConfigurarAtmosferaEMusica(raiz, config);

// Props temáticos da nova zona sob TERRENO/Props_Fase
Transform propsFase = ObterOuCriarFilho(raiz["TERRENO"], "Props_Fase");
DistribuirPropsZona(config.EhFase3, propsFase, terreno, inicioNoTerreno, dir, direita, comp, margem);

// Armadilhas na nova zona
DistribuirArmadilhasZona(config.EhFase3, raiz["ARMADILHAS"], terreno, inicioNoTerreno, dir, direita, comp, margem);

// Coletáveis na nova zona
DistribuirColetaveisZona(config.EhFase3, raiz["COLETAVEIS"], terreno, inicioNoTerreno, dir, direita, comp, margem);

// Inimigos na nova zona
DistribuirInimigosZona(config.EhFase3, raiz["INIMIGOS"], terreno, inicioNoTerreno, dir, direita, comp, margem);

// Objetivo de Relíquias + Portal no fim da zona
ConfigurarObjetivoEPortal(raiz["SISTEMAS"], config, terreno, inicioNoTerreno, dir, comp, margem);

// Salvar antes do bake evita janela manual da Unity para criar pasta de NavMesh
bool salvouInicial = EditorSceneManager.SaveScene(cena, config.CaminhoDestino);
if (!salvouInicial)
Debug.LogError($"[GeradorMenuECenas] Falha ao salvar '{config.CaminhoDestino}' antes do bake do NavMesh.");

Vector3 centroZona = PontoNoTerreno(terreno, inicioNoTerreno + dir * (comp * 0.5f), margem, 0f);
GameObject navObj = new GameObject("NavMesh_Zona");
navObj.transform.SetParent(raiz["SISTEMAS"], false);
navObj.transform.position = centroZona;

NavMeshSurface surface = navObj.AddComponent<NavMeshSurface>();
surface.collectObjects = CollectObjects.Volume;
surface.center = Vector3.zero;
surface.size = new Vector3(comp + 45f, Mathf.Max(80f, tSize.y + 40f), comp + 45f);
surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
surface.BuildNavMesh();

EditorSceneManager.SaveScene(cena, config.CaminhoDestino);
    }

    private static void LimparAtoresGameplayClonados()
{
    foreach (ItemColetavel c in UnityEngine.Object.FindObjectsByType<ItemColetavel>(FindObjectsSortMode.None))
    {
        if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
    }

        foreach (InimigoPerseguidor inimigo in UnityEngine.Object.FindObjectsByType<InimigoPerseguidor>(FindObjectsSortMode.None))
        {
            if (inimigo != null) UnityEngine.Object.DestroyImmediate(inimigo.gameObject);
        }

    foreach (Armadilha armadilha in UnityEngine.Object.FindObjectsByType<Armadilha>(FindObjectsSortMode.None))
    {
        if (armadilha != null) UnityEngine.Object.DestroyImmediate(armadilha.gameObject);
    }

    foreach (PortalProximaFase portal in UnityEngine.Object.FindObjectsByType<PortalProximaFase>(FindObjectsSortMode.None))
    {
        if (portal != null) UnityEngine.Object.DestroyImmediate(portal.gameObject);
    }

    foreach (NavMeshSurface surface in UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
    {
        if (surface != null) UnityEngine.Object.DestroyImmediate(surface);
    }
}

private static GameObject LocalizarOuInstanciarJogador()
{
    GameObject jogadorObj = GameObject.FindWithTag("Player");
    if (jogadorObj == null)
    {
        ControleJogador controle = UnityEngine.Object.FindFirstObjectByType<ControleJogador>();
        if (controle != null)
            jogadorObj = controle.gameObject;
    }

    if (jogadorObj == null)
        jogadorObj = InstanciarPrefabSeExistir(PathPrefabJogador, Vector3.zero, "Prefab_Jogador");

    return jogadorObj;
}

private static void GarantirHudECamera(Dictionary<string, Transform> raiz, bool ehFase3)
{
    if (UnityEngine.Object.FindFirstObjectByType<HUDInventario>() == null)
    {
        GameObject hudObj = InstanciarPrefabSeExistir(PathPrefabHud, Vector3.zero, "Prefab_HUD_Sistema");
        ParentearSeNaoNulo(hudObj, raiz["HUD"]);
    }

    if (Camera.main == null)
    {
        GameObject camObj = InstanciarPrefabSeExistir(PathPrefabCamera, Vector3.zero, "Prefab_Camera");
        ParentearSeNaoNulo(camObj, raiz["CAMERA"]);
    }

    if (ehFase3 && UnityEngine.Object.FindFirstObjectByType<TelaVitoriaUI>() == null)
    {
        GameObject vitoriaObj = InstanciarPrefabSeExistir(PathPrefabVitoria, Vector3.zero, "Prefab_TelaVitoria");
        ParentearSeNaoNulo(vitoriaObj, raiz["HUD"]);
    }
}

private static void ConfigurarAtmosferaEMusica(Dictionary<string, Transform> raiz, ConfigFaseMapa config)
{
    foreach (Light luz in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
    {
        if (luz != null && luz.type == LightType.Directional)
        {
            luz.color = config.CorLuz;
            luz.intensity = config.IntensidadeLuz;
            luz.transform.rotation = Quaternion.Euler(config.RotacaoLuz);
            break;
        }
    }

    RenderSettings.fog = true;
    RenderSettings.fogMode = FogMode.ExponentialSquared;
    RenderSettings.fogColor = config.CorFog;
    RenderSettings.fogDensity = config.DensidadeFog;

    AudioSource musicaExistente = null;
    foreach (AudioSource src in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
    {
        if (src != null && src.loop && src.GetComponentInParent<ControleJogador>() == null)
        {
            musicaExistente = src;
            break;
        }
    }

    if (musicaExistente != null)
    {
        DefinirClipAudio(musicaExistente.gameObject, config.CaminhoMusica);
    }
    else
    {
        GameObject bgmObj = InstanciarPrefabSeExistir(PathPrefabMusica, Vector3.zero, "Prefab_MusicaFundo");
        DefinirClipAudio(bgmObj, config.CaminhoMusica);
        ParentearSeNaoNulo(bgmObj, raiz["AUDIO"]);
    }
}

private static void DistribuirPropsZona(
    bool ehFase3,
    Transform paiProps,
    Terrain terreno,
    Vector3 inicio,
    Vector3 dir,
    Vector3 direita,
    float comp,
    float margem)
{
    if (!ehFase3)
    {
        InstanciarPropNoTerreno("Assets/Mountain Terrain rocks and tree/Prefab/rock_set_01.prefab", paiProps, terreno, inicio + dir * (comp * 0.25f) - direita * 11f, new Vector3(0f, 35f, 0f), margem);
        InstanciarPropNoTerreno("Assets/Mountain Terrain rocks and tree/Prefab/rock_set_02.prefab", paiProps, terreno, inicio + dir * (comp * 0.55f) + direita * 11f, new Vector3(0f, -45f, 0f), margem);
        InstanciarPropNoTerreno("Assets/Mountain Terrain rocks and tree/Prefab/rock_set_03.prefab", paiProps, terreno, inicio + dir * (comp * 0.85f) - direita * 10f, new Vector3(0f, 90f, 0f), margem);
        InstanciarPropNoTerreno("Assets/Assets_FenceChained/prefabs/chainlink_group-1.prefab", paiProps, terreno, inicio + dir * (comp * 0.45f) - direita * 14f, Quaternion.LookRotation(dir).eulerAngles, margem);
        InstanciarPropNoTerreno("Assets/Assets_FenceChained/prefabs/chainlink_group-1.prefab", paiProps, terreno, inicio + dir * (comp * 0.45f) + direita * 14f, Quaternion.LookRotation(dir).eulerAngles, margem);
    }
    else
    {
        InstanciarPropNoTerreno("Assets/Damaged Models pack1/Prefabs/trolleybus_dmg (1).prefab", paiProps, terreno, inicio + dir * (comp * 0.30f) - direita * 10f, new Vector3(0f, 25f, 0f), margem);
        InstanciarPropNoTerreno("Assets/Damaged Models pack1/Prefabs/kiosk_dmg.prefab", paiProps, terreno, inicio + dir * (comp * 0.55f) + direita * 10f, new Vector3(0f, -35f, 0f), margem);
        InstanciarPropNoTerreno("Assets/Damaged Models pack1/Prefabs/lamppost.prefab", paiProps, terreno, inicio + dir * (comp * 0.70f) - direita * 8f, Vector3.zero, margem);
        InstanciarPropNoTerreno("Assets/Damaged Models pack1/Prefabs/sign_dmg.prefab", paiProps, terreno, inicio + dir * (comp * 0.20f) + direita * 7f, new Vector3(0f, 140f, 0f), margem);
        InstanciarPropNoTerreno("Assets/Road props for games/Prefabs/BarrierFence.prefab", paiProps, terreno, inicio + dir * (comp * 0.45f) - direita * 13f, Quaternion.LookRotation(dir).eulerAngles, margem);
        InstanciarPropNoTerreno("Assets/Road props for games/Prefabs/BarrierFence.prefab", paiProps, terreno, inicio + dir * (comp * 0.45f) + direita * 13f, Quaternion.LookRotation(dir).eulerAngles, margem);
        InstanciarPropNoTerreno("Assets/Mountain Terrain rocks and tree/Prefab/rock_set_04.prefab", paiProps, terreno, inicio + dir * (comp * 0.85f) + direita * 11f, Vector3.zero, margem);
    }
}

private static void DistribuirArmadilhasZona(
    bool ehFase3,
    Transform paiArmadilhas,
    Terrain terreno,
    Vector3 inicio,
    Vector3 dir,
    Vector3 direita,
    float comp,
    float margem)
{
    string[] prefabsTrap =
    {
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Grid.prefab",
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Round.prefab",
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Star.prefab"
        };

        if (!ehFase3)
    {
    CriarArmadilha(prefabsTrap[0], paiArmadilhas, PontoNoTerreno(terreno, inicio + dir * (comp * 0.24f), margem, 0.05f), 20f);
    CriarArmadilha(prefabsTrap[1], paiArmadilhas, PontoNoTerreno(terreno, inicio + dir * (comp * 0.50f) - direita * 4f, margem, 0.05f), 25f);
    CriarArmadilha(prefabsTrap[2], paiArmadilhas, PontoNoTerreno(terreno, inicio + dir * (comp * 0.74f) + direita * 4f, margem, 0.05f), 25f);
}
        else
{
    CriarArmadilha(prefabsTrap[0], paiArmadilhas, PontoNoTerreno(terreno, inicio + dir * (comp * 0.20f) - direita * 3.5f, margem, 0.05f), 25f);
    CriarArmadilha(prefabsTrap[1], paiArmadilhas, PontoNoTerreno(terreno, inicio + dir * (comp * 0.42f) + direita * 4.0f, margem, 0.05f), 25f);
    CriarArmadilha(prefabsTrap[2], paiArmadilhas, PontoNoTerreno(terreno, inicio + dir * (comp * 0.64f) - direita * 4.0f, margem, 0.05f), 30f);
    CriarArmadilha(prefabsTrap[0], paiArmadilhas, PontoNoTerreno(terreno, inicio + dir * (comp * 0.84f) + direita * 3.5f, margem, 0.05f), 30f);
}
    }

    private static void DistribuirColetaveisZona(
        bool ehFase3,
        Transform raizColetaveis,
        Terrain terreno,
        Vector3 inicio,
        Vector3 dir,
        Vector3 direita,
        float comp,
        float margem)
{
    Transform subMoedas = ObterOuCriarFilho(raizColetaveis, "_MOEDAS");
    Transform subReliquias = ObterOuCriarFilho(raizColetaveis, "_RELIQUIAS");
    Transform subPocoes = ObterOuCriarFilho(raizColetaveis, "_POCOES");

    Vector3[] reliquias =
    {
            PontoNoTerreno(terreno, inicio + dir * (comp * 0.18f) - direita * 6.5f, margem, 0.85f),
            PontoNoTerreno(terreno, inicio + dir * (comp * 0.36f) + direita * 6.5f, margem, 0.85f),
            PontoNoTerreno(terreno, inicio + dir * (comp * 0.54f), margem, 0.85f),
            PontoNoTerreno(terreno, inicio + dir * (comp * 0.72f) - direita * 7.0f, margem, 0.85f),
            PontoNoTerreno(terreno, inicio + dir * (comp * 0.88f) + direita * 6.0f, margem, 0.85f)
        };
    InstanciarColetaveisNoGrupo($"{PastaPrefabsColetaveis}/Coletavel_Reliquia.prefab", subReliquias, reliquias);

    int qtdMoedas = ehFase3 ? 8 : 6;
    Vector3[] moedas = new Vector3[qtdMoedas];
    for (int i = 0; i < qtdMoedas; i++)
    {
    float t = Mathf.Lerp(0.12f, 0.92f, (float)i / Mathf.Max(1, qtdMoedas - 1));
    float zig = (i % 2 == 0 ? 1f : -1f) * 2.5f;
    moedas[i] = PontoNoTerreno(terreno, inicio + dir * (comp * t) + direita * zig, margem, 0.85f);
}
InstanciarColetaveisNoGrupo($"{PastaPrefabsColetaveis}/Coletavel_Moeda.prefab", subMoedas, moedas);

Vector3[] pocoes = ehFase3
            ? new[]
            {
                PontoNoTerreno(terreno, inicio + dir * (comp * 0.25f) + direita * 7.5f, margem, 0.85f),
                PontoNoTerreno(terreno, inicio + dir * (comp * 0.58f) - direita * 7.5f, margem, 0.85f),
                PontoNoTerreno(terreno, inicio + dir * (comp * 0.80f) + direita * 7.0f, margem, 0.85f)
            }
            : new[]
            {
                PontoNoTerreno(terreno, inicio + dir * (comp * 0.30f) + direita * 7.0f, margem, 0.85f),
                PontoNoTerreno(terreno, inicio + dir * (comp * 0.68f) - direita * 7.0f, margem, 0.85f)
            };
InstanciarColetaveisNoGrupo($"{PastaPrefabsColetaveis}/Coletavel_Pocao.prefab", subPocoes, pocoes);
    }

    private static void DistribuirInimigosZona(
        bool ehFase3,
        Transform paiInimigos,
        Terrain terreno,
        Vector3 inicio,
        Vector3 dir,
        Vector3 direita,
        float comp,
        float margem)
{
    Quaternion olhandoParaInicio = Quaternion.LookRotation(-dir);

    if (!ehFase3)
    {
    Vector3 pos = PontoNoTerreno(terreno, inicio + dir * (comp * 0.65f), margem, 0f);
    CriarInimigoFase("Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin2.prefab", paiInimigos, "Inimigo_Guardiao_Ruinas", pos, olhandoParaInicio);
}
        else
{
    Vector3 pos1 = PontoNoTerreno(terreno, inicio + dir * (comp * 0.50f) - direita * 5f, margem, 0f);
    Vector3 pos2 = PontoNoTerreno(terreno, inicio + dir * (comp * 0.80f) + direita * 5f, margem, 0f);
    CriarInimigoFase("Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin1.prefab", paiInimigos, "Inimigo_Nave_Alfa", pos1, olhandoParaInicio);
    CriarInimigoFase("Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin3.prefab", paiInimigos, "Inimigo_Nave_Omega", pos2, olhandoParaInicio);
}
    }

    private static void ConfigurarObjetivoEPortal(
        Transform paiSistemas,
        ConfigFaseMapa config,
        Terrain terreno,
        Vector3 inicio,
        Vector3 dir,
        float comp,
        float margem)
{
    ObjetivoReliquias objetivo = UnityEngine.Object.FindFirstObjectByType<ObjetivoReliquias>();
    if (objetivo == null)
    {
    GameObject objFase = new GameObject("Objetivo_Fase");
    objFase.transform.SetParent(paiSistemas, false);
    objetivo = objFase.AddComponent<ObjetivoReliquias>();
}

SerializedObject soObj = new SerializedObject(objetivo);
SerializedProperty propMeta = soObj.FindProperty("meta");
if (propMeta != null)
    propMeta.intValue = 5;
soObj.ApplyModifiedProperties();

Vector3 posPortal = PontoNoTerreno(terreno, inicio + dir * comp, margem, 0f);
string nomePortal = config.PortalFinal ? "Portal_Final_Tesouro" : "Portal_Para_Fase3";
GameObject portalObj = InstanciarPrefabSeExistir(PathPrefabPortal, posPortal, nomePortal);
if (portalObj != null)
{
    portalObj.transform.rotation = Quaternion.LookRotation(-dir);
    portalObj.transform.SetParent(paiSistemas, true);
    ConfigurarPortal(portalObj, config.CenaDestinoPortal, config.PortalFinal, exigirReliquias: true);
}
    }

private static Vector3 PontoNoTerreno(Terrain terreno, Vector3 posXZ, float margem, float offsetY)
{
    if (terreno == null || terreno.terrainData == null)
        return new Vector3(posXZ.x, offsetY, posXZ.z);

    Vector3 tPos = terreno.transform.position;
    Vector3 tSize = terreno.terrainData.size;

    float x = Mathf.Clamp(posXZ.x, tPos.x + margem, tPos.x + Mathf.Max(margem, tSize.x - margem));
    float z = Mathf.Clamp(posXZ.z, tPos.z + margem, tPos.z + Mathf.Max(margem, tSize.z - margem));
    Vector3 amostra = new Vector3(x, posXZ.y, z);
    float y = terreno.SampleHeight(amostra) + tPos.y + offsetY;
    return new Vector3(x, y, z);
}

private static void InstanciarPropNoTerreno(
    string caminhoPrefab,
    Transform pai,
    Terrain terreno,
    Vector3 posXZ,
    Vector3 eulerRot,
    float margem)
{
    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
    if (prefab == null)
        return;

    GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    if (instancia == null)
        return;

    instancia.transform.SetParent(pai, false);
    instancia.transform.rotation = Quaternion.Euler(eulerRot);
    instancia.transform.position = PontoNoTerreno(terreno, posXZ, margem, 0f);
    AssentarPorBoundsNoTerreno(instancia, terreno, margem);
}

private static void AssentarPorBoundsNoTerreno(GameObject obj, Terrain terreno, float margem)
{
    if (obj == null)
        return;

    Vector3 posChao = PontoNoTerreno(terreno, obj.transform.position, margem, 0f);
    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(false);
    Bounds boundsCombinado = default;
    bool temBounds = false;

    foreach (Renderer r in renderers)
    {
        if (r == null || !r.enabled || r.bounds.size == Vector3.zero)
            continue;

        if (!temBounds)
        {
            boundsCombinado = r.bounds;
            temBounds = true;
        }
        else
        {
            boundsCombinado.Encapsulate(r.bounds);
        }
    }

    if (temBounds)
    {
        float offsetPivot = obj.transform.position.y - boundsCombinado.min.y;
        posChao.y += offsetPivot;
    }

    obj.transform.position = posChao;
}

private static int RegistrarCenasNoBuildSettings(bool silencioso)
{
    string[] caminhosOficiais =
    {
            PathCenaMenu,
            PathCenaFase1,
            PathCenaFase2,
            PathCenaFase3
        };

    List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>();
    List<string> faltantes = new List<string>();

    foreach (string caminho in caminhosOficiais)
    {
        if (File.Exists(caminho))
            lista.Add(new EditorBuildSettingsScene(caminho, true));
        else
            faltantes.Add(caminho);
    }

    EditorBuildSettings.scenes = lista.ToArray();

    if (!silencioso)
    {
        string avisoFaltantes = faltantes.Count > 0
            ? "\n\n⚠ Cenas ainda não encontradas no disco:\n- " + string.Join("\n- ", faltantes)
            : string.Empty;

        EditorUtility.DisplayDialog(
            "Build Settings Atualizado",
            $"Registradas {lista.Count}/4 cenas na ordem oficial (0..3) em ProjectSettings/EditorBuildSettings.asset." + avisoFaltantes,
            "OK");
    }

    return lista.Count;
}

private static GameObject InstanciarPrefabSeExistir(string caminhoAsset, Vector3 pos, string nomeInstancia)
{
    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoAsset);
    if (prefab == null)
    {
        Debug.LogWarning($"[GeradorMenuECenas] Prefab não encontrado em '{caminhoAsset}'.");
        return null;
    }

    GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    if (instancia != null)
    {
        instancia.name = nomeInstancia;
        instancia.transform.position = pos;
    }

    return instancia;
}

private static void DefinirClipAudio(GameObject objAudio, string caminhoClip)
{
    if (objAudio == null || string.IsNullOrEmpty(caminhoClip))
        return;

    AudioSource src = objAudio.GetComponent<AudioSource>();
    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(caminhoClip);
    if (src != null && clip != null)
    {
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = true;
        src.spatialBlend = 0f;
        src.volume = 0.35f;
    }
}

    private static void CriarArmadilha(string caminhoPrefab, Transform pai, Vector3 pos, float dano)
{
    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
    GameObject trapObj;

    if (prefab != null)
    {
        trapObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        trapObj.name = "Armadilha_Chao";
    }
    else
    {
        trapObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trapObj.name = "Armadilha_Chao";
        trapObj.transform.localScale = new Vector3(2.2f, 0.1f, 2.2f);
    }

    trapObj.transform.SetParent(pai, false);
    trapObj.transform.position = pos;

    BoxCollider box = trapObj.GetComponent<BoxCollider>();
    if (box == null)
        box = trapObj.AddComponent<BoxCollider>();
    box.isTrigger = true;
    box.center = new Vector3(0f, 0.4f, 0f);
    box.size = new Vector3(2.2f, 1.0f, 2.2f);

    Armadilha armadilha = trapObj.GetComponent<Armadilha>() ?? trapObj.AddComponent<Armadilha>();
    SerializedObject so = new SerializedObject(armadilha);
    SerializedProperty propDano = so.FindProperty("dano");
    if (propDano != null) propDano.floatValue = dano;

    AudioClip somDano = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/SFX_Dano_Impacto.wav");
    SerializedProperty propSom = so.FindProperty("somAtivacao");
    if (propSom != null && somDano != null) propSom.objectReferenceValue = somDano;
    so.ApplyModifiedProperties();
}

    private static void InstanciarColetaveisNoGrupo(string caminhoPrefab, Transform pai, Vector3[] posicoes)
{
    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
    if (prefab == null)
        return;

    foreach (Vector3 pos in posicoes)
    {
        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instancia != null)
        {
            instancia.transform.SetParent(pai, true);
            instancia.transform.position = pos;
        }
    }
}

    private static void CriarInimigoFase(string caminhoPrefab, Transform pai, string nome, Vector3 pos, Quaternion rotacao)
{
    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
    GameObject inimigoObj;

    if (prefab != null)
    {
        inimigoObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inimigoObj.name = nome;

        Animator anim = inimigoObj.GetComponentInChildren<Animator>();
        if (anim != null && anim.avatar == null)
        {
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/SciFi Beast06 Bull/Mesh/SciFi Beast06 Bull Skin1.fbx");
            if (avatar != null)
                anim.avatar = avatar;
        }
    }
    else
    {
        inimigoObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        inimigoObj.name = nome;
    }

    inimigoObj.transform.SetParent(pai, true);
    inimigoObj.transform.SetPositionAndRotation(pos, rotacao);

    NavMeshAgent agente = inimigoObj.GetComponent<NavMeshAgent>() ?? inimigoObj.AddComponent<NavMeshAgent>();
    agente.speed = 3.5f;
    agente.stoppingDistance = 1.2f;
    agente.radius = 0.8f;
    agente.height = 2.0f;

    InimigoPerseguidor seguidor = inimigoObj.GetComponent<InimigoPerseguidor>() ?? inimigoObj.AddComponent<InimigoPerseguidor>();
    AudioClip somAtaque = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/SFX_Ataque_Inimigo.wav");
    if (somAtaque != null)
    {
        SerializedObject so = new SerializedObject(seguidor);
        SerializedProperty propSom = so.FindProperty("somAtaque");
        if (propSom != null)
        {
            propSom.objectReferenceValue = somAtaque;
            so.ApplyModifiedProperties();
        }
    }
}

private static void ConfigurarPortal(GameObject portalObj, string cenaDestino, bool ehFinal, bool exigirReliquias)
{
    if (portalObj == null)
        return;

    PortalProximaFase portal = portalObj.GetComponent<PortalProximaFase>();
    if (portal == null)
        return;

    SerializedObject so = new SerializedObject(portal);
    so.FindProperty("nomeCenaDestino").stringValue = cenaDestino;
    so.FindProperty("ehFinalDoJogo").boolValue = ehFinal;
    so.FindProperty("exigirObjetivoReliquias").boolValue = exigirReliquias;
    so.ApplyModifiedProperties();
}

    private static Dictionary<string, Transform> CriarEstruturaRaiz(Scene cena)
{
    string[] nomes = { "CAMERA", "LUZ", "TERRENO", "PLAYER", "HUD", "COLETAVEIS", "INIMIGOS", "ARMADILHAS", "SISTEMAS", "AUDIO" };
    Dictionary<string, Transform> existente = new Dictionary<string, Transform>(StringComparer.Ordinal);

        foreach (GameObject root in cena.GetRootGameObjects())
        {
            if (root != null && !existente.ContainsKey(root.name))
                existente[root.name] = root.transform;
        }

        Dictionary<string, Transform> raiz = new Dictionary<string, Transform>(StringComparer.Ordinal);
    for (int i = 0; i < nomes.Length; i++)
    {
            if (!existente.TryGetValue(nomes[i], out Transform container) || container == null)
            {
                GameObject novo = new GameObject(nomes[i]);
                container = novo.transform;
            }
        container.SetSiblingIndex(i);
        raiz[nomes[i]] = container;
    }

    return raiz;
}

    private static Transform ObterOuCriarFilho(Transform pai, string nome)
{
    Transform existente = pai.Find(nome);
    if (existente != null)
        return existente;

    GameObject sub = new GameObject(nome);
    sub.transform.SetParent(pai, false);
    return sub.transform;
}

private static void ParentearSeNaoNulo(GameObject obj, Transform pai)
{
    if (obj != null && pai != null)
        obj.transform.SetParent(pai, true);
}

private static GameObject CriarPainelUI(Transform pai, string nome, Color cor)
{
    GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
    obj.layer = 5;
    obj.transform.SetParent(pai, false);
    Image img = obj.GetComponent<Image>();
    img.color = cor;
    return obj;
}

private static TextMeshProUGUI CriarTextoUI(
    Transform pai,
    string nome,
    string conteudo,
    Vector2 pos,
    Vector2 tamanho,
    float fontSize,
    FontStyles estilo,
    Color cor)
{
    GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
    obj.layer = 5;
    obj.transform.SetParent(pai, false);

    RectTransform rt = obj.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 0.5f);
    rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.sizeDelta = tamanho;
    rt.anchoredPosition = pos;

    TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
    tmp.text = conteudo;
    tmp.fontSize = fontSize;
    tmp.fontStyle = estilo;
    tmp.color = cor;
    tmp.alignment = TextAlignmentOptions.Center;
    return tmp;
}

private static Button CriarBotaoUI(
    Transform pai,
    string nome,
    string rotulo,
    Vector2 pos,
    Vector2 tamanho,
    Color corFundo)
{
    GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
    obj.layer = 5;
    obj.transform.SetParent(pai, false);

    RectTransform rt = obj.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 0.5f);
    rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.sizeDelta = tamanho;
    rt.anchoredPosition = pos;

    Image img = obj.GetComponent<Image>();
    img.color = corFundo;

    Button btn = obj.GetComponent<Button>();
    ColorBlock cb = btn.colors;
    cb.normalColor = Color.white;
    cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
    cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    btn.colors = cb;

    CriarTextoUI(obj.transform, "Texto", rotulo, Vector2.zero, tamanho, 22f, FontStyles.Bold, Color.white);
    return btn;
}
}
