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
#if SORTIFY
using Sortify;
#endif

/// <summary>
/// One-click Editor generator for Stage 4: modular phase prefabs (Portal, BGM, Victory),
/// Cena_MenuPrincipal, Cena_Fase2_Ruinas, Cena_Fase3_Nave, and Build Settings registration (0..3).
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

    private const string PathCenaMenu = PastaCenas + "/Cena_MenuPrincipal.unity";
    private const string PathCenaFase1 = PastaCenas + "/Cena_principal.unity";
    private const string PathCenaFase2 = PastaCenas + "/Cena_Fase2_Ruinas.unity";
    private const string PathCenaFase3 = PastaCenas + "/Cena_Fase3_Nave.unity";

    private const string PathMusicaMenu = "Assets/Desert Music Pack/Loops/1. Dunes of Silence.wav";
    private const string PathMusicaFase2 = "Assets/Desert Music Pack/Loops/3. Whispers in the Sand.wav";
    private const string PathMusicaFase3 = "Assets/Desert Music Pack/Loops/4. Canyon Echoes.wav";

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

        EditorUtility.DisplayProgressBar("Aventura Inca", "Montando Cena_MenuPrincipal...", 0.25f);
        CriarCenaMenuPrincipal();

        EditorUtility.DisplayProgressBar("Aventura Inca", "Montando Cena_Fase2_Ruinas...", 0.55f);
        CriarCenaFase2Ruinas();

        EditorUtility.DisplayProgressBar("Aventura Inca", "Montando Cena_Fase3_Nave...", 0.85f);
        CriarCenaFase3Nave();

        EditorUtility.DisplayProgressBar("Aventura Inca", "Registrando Build Settings (0..3)...", 0.95f);
        int registradas = RegistrarCenasNoBuildSettings(silencioso: true);

        // Abrir o Menu Principal ao final para inspeção/teste imediato
        EditorSceneManager.OpenScene(PathCenaMenu, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog(
            "Etapa 4 Gerada com Sucesso!",
            $"✔ Cena_MenuPrincipal.unity criada (Build 0)\n" +
            $"✔ Cena_Fase2_Ruinas.unity criada com NavMesh, armadilhas, inimigo e portal (Build 2)\n" +
            $"✔ Cena_Fase3_Nave.unity criada com destroços, desafio final, portal final e Tela de Vitória (Build 3)\n" +
            $"✔ {registradas}/4 cenas registradas no EditorBuildSettings\n\n" +
            "Nota (ADR-04): Cena_principal.unity (Fase 1) NÃO foi editada diretamente.\n" +
            "Quando o responsável integrá-la, basta arrastar Prefab_PortalProximaFase (destino: Cena_Fase2_Ruinas) e Prefab_MusicaFundo para a Fase 1.\n\n" +
            "Pressione PLAY na Cena_MenuPrincipal para testar o fluxo!",
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
        if (bgmObj != null)
        {
            AudioSource src = bgmObj.GetComponent<AudioSource>();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PathMusicaMenu);
            if (src != null && clip != null)
                src.clip = clip;
        }

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

        // Painel Principal
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

        // Painel Seleção de Fases
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

        OrganizarCenaComSortify(cena, camObj, null, null, null, canvasObj, null, null, null, esObj, bgmObj);

        EditorSceneManager.SaveScene(cena, PathCenaMenu);
    }

    private static void CriarCenaFase2Ruinas()
    {
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        RemoverMainCameraPadrao();

        Light luz = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (luz != null)
        {
            luz.color = new Color(1f, 0.9f, 0.74f);
            luz.intensity = 1.25f;
        }

        GameObject camObj = InstanciarPrefabSeExistir(PathPrefabCamera, Vector3.zero, "Prefab_Camera");
        GameObject jogadorObj = InstanciarPrefabSeExistir(PathPrefabJogador, new Vector3(0f, 0.1f, -26f), "Prefab_Jogador");
        GameObject hudObj = InstanciarPrefabSeExistir(PathPrefabHud, Vector3.zero, "Prefab_HUD_Sistema");

        GameObject bgmObj = InstanciarPrefabSeExistir(PathPrefabMusica, Vector3.zero, "Prefab_MusicaFundo");
        DefinirClipAudio(bgmObj, PathMusicaFase2);

        GameObject terrenoRaiz = CriarArenaBase(
            "Solo_Ruinas",
            new Vector3(70f, 1f, 90f),
            CriarOuObterMaterialURP("Mat_Solo_Ruinas", new Color(0.56f, 0.46f, 0.34f), 0f, 0.25f));

        // Decoração de ruínas e cercas
        string[] cercas =
        {
            "Assets/Assets_FenceChained/prefabs/chainlink_group-1.prefab",
            "Assets/Mountain Terrain rocks and tree/Prefab/rock_set_01.prefab",
            "Assets/Mountain Terrain rocks and tree/Prefab/rock_set_02.prefab",
            "Assets/Mountain Terrain rocks and tree/Prefab/rock_set_03.prefab"
        };
        InstanciarPropSegura(cercas[1], terrenoRaiz.transform, new Vector3(-16f, 0f, -8f), new Vector3(0f, 35f, 0f));
        InstanciarPropSegura(cercas[2], terrenoRaiz.transform, new Vector3(16f, 0f, 6f), new Vector3(0f, -45f, 0f));
        InstanciarPropSegura(cercas[3], terrenoRaiz.transform, new Vector3(-14f, 0f, 20f), new Vector3(0f, 90f, 0f));
        InstanciarPropSegura(cercas[0], terrenoRaiz.transform, new Vector3(-22f, 0f, 0f), new Vector3(0f, 90f, 0f));
        InstanciarPropSegura(cercas[0], terrenoRaiz.transform, new Vector3(22f, 0f, 0f), new Vector3(0f, 90f, 0f));

        // Armadilhas (Dungeon Floor Traps)
        string[] prefabsTrap =
        {
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Grid.prefab",
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Round.prefab",
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Star.prefab"
        };
        List<GameObject> armadilhas = new List<GameObject>
        {
            CriarArmadilha(prefabsTrap[0], new Vector3(0f, 0.05f, -12f), 20f),
            CriarArmadilha(prefabsTrap[1], new Vector3(-5f, 0.05f, 4f), 25f),
            CriarArmadilha(prefabsTrap[2], new Vector3(5f, 0.05f, 18f), 25f)
        };

        // Coletáveis (5 Relíquias, 6 Moedas, 2 Poções)
        List<GameObject> coletaveis = new List<GameObject>();
        Vector3[] posReliquias =
        {
            new Vector3(-9f, 0.85f, -14f),
            new Vector3(9f, 0.85f, -4f),
            new Vector3(0f, 0.85f, 8f),
            new Vector3(-10f, 0.85f, 18f),
            new Vector3(8f, 0.85f, 24f)
        };
        AdicionarColetaveis(coletaveis, $"{PastaPrefabsColetaveis}/Coletavel_Reliquia.prefab", posReliquias);

        Vector3[] posMoedas =
        {
            new Vector3(0f, 0.85f, -20f),
            new Vector3(-3f, 0.85f, -6f),
            new Vector3(3f, 0.85f, 0f),
            new Vector3(-3f, 0.85f, 12f),
            new Vector3(3f, 0.85f, 20f),
            new Vector3(0f, 0.85f, 27f)
        };
        AdicionarColetaveis(coletaveis, $"{PastaPrefabsColetaveis}/Coletavel_Moeda.prefab", posMoedas);

        Vector3[] posPocoes =
        {
            new Vector3(8f, 0.85f, -16f),
            new Vector3(-8f, 0.85f, 12f)
        };
        AdicionarColetaveis(coletaveis, $"{PastaPrefabsColetaveis}/Coletavel_Pocao.prefab", posPocoes);

        // Inimigo
        List<GameObject> inimigos = new List<GameObject>
        {
            CriarInimigoFase("Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin2.prefab", "Inimigo_Guardiao_Ruinas", new Vector3(0f, 0f, 12f))
        };

        // Sistemas: Objetivo + Portal -> Fase 3
        GameObject objFase = CriarObjetoObjetivo(5);
        GameObject portalObj = InstanciarPrefabSeExistir(PathPrefabPortal, new Vector3(0f, 0f, 32f), "Portal_Para_Fase3");
        ConfigurarPortal(portalObj, "Cena_Fase3_Nave", ehFinal: false, exigirReliquias: true);

        // Bake NavMesh antes de salvar
        NavMeshSurface surface = terrenoRaiz.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();

        List<GameObject> sistemas = new List<GameObject> { objFase, portalObj };
        OrganizarCenaComSortify(cena, camObj, luz != null ? luz.gameObject : null, terrenoRaiz, jogadorObj, hudObj, coletaveis, inimigos, armadilhas, sistemas, bgmObj);

        EditorSceneManager.SaveScene(cena, PathCenaFase2);
    }

    private static void CriarCenaFase3Nave()
    {
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        RemoverMainCameraPadrao();

        Light luz = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (luz != null)
        {
            luz.color = new Color(0.92f, 0.86f, 0.78f);
            luz.intensity = 1.15f;
        }

        GameObject camObj = InstanciarPrefabSeExistir(PathPrefabCamera, Vector3.zero, "Prefab_Camera");
        GameObject jogadorObj = InstanciarPrefabSeExistir(PathPrefabJogador, new Vector3(0f, 0.1f, -28f), "Prefab_Jogador");
        GameObject hudObj = InstanciarPrefabSeExistir(PathPrefabHud, Vector3.zero, "Prefab_HUD_Sistema");
        GameObject vitoriaObj = InstanciarPrefabSeExistir(PathPrefabVitoria, Vector3.zero, "Prefab_TelaVitoria");

        GameObject bgmObj = InstanciarPrefabSeExistir(PathPrefabMusica, Vector3.zero, "Prefab_MusicaFundo");
        DefinirClipAudio(bgmObj, PathMusicaFase3);

        GameObject terrenoRaiz = CriarArenaBase(
            "Solo_Destrocos_Nave",
            new Vector3(80f, 1f, 96f),
            CriarOuObterMaterialURP("Mat_Solo_Nave", new Color(0.36f, 0.31f, 0.26f), 0.12f, 0.35f));

        // Destroços da nave / cenário danificado
        InstanciarPropSegura("Assets/Damaged Models pack1/Prefabs/trolleybus_dmg (1).prefab", terrenoRaiz.transform, new Vector3(-13f, 0f, -4f), new Vector3(0f, 25f, 0f));
        InstanciarPropSegura("Assets/Damaged Models pack1/Prefabs/kiosk_dmg.prefab", terrenoRaiz.transform, new Vector3(14f, 0f, 10f), new Vector3(0f, -35f, 0f));
        InstanciarPropSegura("Assets/Damaged Models pack1/Prefabs/lamppost.prefab", terrenoRaiz.transform, new Vector3(-8f, 0f, 16f), Vector3.zero);
        InstanciarPropSegura("Assets/Damaged Models pack1/Prefabs/sign_dmg.prefab", terrenoRaiz.transform, new Vector3(7f, 0f, -15f), new Vector3(0f, 140f, 0f));
        InstanciarPropSegura("Assets/Road props for games/Prefabs/BarrierFence.prefab", terrenoRaiz.transform, new Vector3(-20f, 0f, 0f), new Vector3(0f, 90f, 0f));
        InstanciarPropSegura("Assets/Road props for games/Prefabs/BarrierFence.prefab", terrenoRaiz.transform, new Vector3(20f, 0f, 0f), new Vector3(0f, 90f, 0f));
        InstanciarPropSegura("Assets/Mountain Terrain rocks and tree/Prefab/rock_set_04.prefab", terrenoRaiz.transform, new Vector3(15f, 0f, -20f), Vector3.zero);

        // 4 Armadilhas
        string[] prefabsTrap =
        {
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Grid.prefab",
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Round.prefab",
            "Assets/Dungeon Floor Traps/Prefabs/PBR/Dungeon_Floor_Trap_Star.prefab"
        };
        List<GameObject> armadilhas = new List<GameObject>
        {
            CriarArmadilha(prefabsTrap[0], new Vector3(-4f, 0.05f, -14f), 25f),
            CriarArmadilha(prefabsTrap[1], new Vector3(5f, 0.05f, -2f), 25f),
            CriarArmadilha(prefabsTrap[2], new Vector3(-5f, 0.05f, 12f), 30f),
            CriarArmadilha(prefabsTrap[0], new Vector3(4f, 0.05f, 22f), 30f)
        };

        // Coletáveis (5 Relíquias, 8 Moedas, 3 Poções)
        List<GameObject> coletaveis = new List<GameObject>();
        Vector3[] posReliquias =
        {
            new Vector3(-10f, 0.85f, -18f),
            new Vector3(10f, 0.85f, -8f),
            new Vector3(0f, 0.85f, 4f),
            new Vector3(-11f, 0.85f, 18f),
            new Vector3(10f, 0.85f, 26f)
        };
        AdicionarColetaveis(coletaveis, $"{PastaPrefabsColetaveis}/Coletavel_Reliquia.prefab", posReliquias);

        Vector3[] posMoedas =
        {
            new Vector3(0f, 0.85f, -22f),
            new Vector3(-4f, 0.85f, -10f),
            new Vector3(4f, 0.85f, -4f),
            new Vector3(-4f, 0.85f, 6f),
            new Vector3(4f, 0.85f, 12f),
            new Vector3(-3f, 0.85f, 24f),
            new Vector3(3f, 0.85f, 28f),
            new Vector3(0f, 0.85f, 31f)
        };
        AdicionarColetaveis(coletaveis, $"{PastaPrefabsColetaveis}/Coletavel_Moeda.prefab", posMoedas);

        Vector3[] posPocoes =
        {
            new Vector3(9f, 0.85f, -18f),
            new Vector3(-8f, 0.85f, 6f),
            new Vector3(8f, 0.85f, 18f)
        };
        AdicionarColetaveis(coletaveis, $"{PastaPrefabsColetaveis}/Coletavel_Pocao.prefab", posPocoes);

        // 2 Inimigos
        List<GameObject> inimigos = new List<GameObject>
        {
            CriarInimigoFase("Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin1.prefab", "Inimigo_Nave_Alfa", new Vector3(-6f, 0f, 10f)),
            CriarInimigoFase("Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin3.prefab", "Inimigo_Nave_Omega", new Vector3(6f, 0f, 24f))
        };

        // Sistemas: Objetivo + Portal Final (Vitória)
        GameObject objFase = CriarObjetoObjetivo(5);
        GameObject portalObj = InstanciarPrefabSeExistir(PathPrefabPortal, new Vector3(0f, 0f, 36f), "Portal_Final_Tesouro");
        ConfigurarPortal(portalObj, string.Empty, ehFinal: true, exigirReliquias: true);

        NavMeshSurface surface = terrenoRaiz.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();

        List<GameObject> hudObjs = new List<GameObject>();
        if (hudObj != null) hudObjs.Add(hudObj);
        if (vitoriaObj != null) hudObjs.Add(vitoriaObj);

        List<GameObject> sistemas = new List<GameObject> { objFase, portalObj };
        OrganizarCenaComSortify(cena, camObj, luz != null ? luz.gameObject : null, terrenoRaiz, jogadorObj, hudObjs, coletaveis, inimigos, armadilhas, sistemas, bgmObj);

        EditorSceneManager.SaveScene(cena, PathCenaFase3);
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
            {
                lista.Add(new EditorBuildSettingsScene(caminho, true));
            }
            else
            {
                faltantes.Add(caminho);
            }
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

    private static void RemoverMainCameraPadrao()
    {
        Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in cameras)
        {
            if (cam != null && cam.CompareTag("MainCamera"))
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
        }
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
        }
    }

    private static GameObject CriarArenaBase(string nomeChao, Vector3 escala, Material matSolo)
    {
        GameObject chao = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chao.name = nomeChao;
        chao.transform.position = new Vector3(0f, -0.5f, 4f);
        chao.transform.localScale = escala;
        chao.isStatic = true;

        Renderer r = chao.GetComponent<Renderer>();
        if (r != null && matSolo != null)
            r.sharedMaterial = matSolo;

        return chao;
    }

    private static void InstanciarPropSegura(string caminhoPrefab, Transform pai, Vector3 pos, Vector3 eulerRot)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
        if (prefab == null)
            return;

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instancia == null)
            return;

        instancia.transform.SetParent(pai, false);
        instancia.transform.position = pos;
        instancia.transform.rotation = Quaternion.Euler(eulerRot);
    }

    private static GameObject CriarArmadilha(string caminhoPrefab, Vector3 pos, float dano)
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

        return trapObj;
    }

    private static void AdicionarColetaveis(List<GameObject> destino, string caminhoPrefab, Vector3[] posicoes)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
        if (prefab == null)
            return;

        foreach (Vector3 pos in posicoes)
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instancia != null)
            {
                instancia.transform.position = pos;
                destino.Add(instancia);
            }
        }
    }

    private static GameObject CriarInimigoFase(string caminhoPrefab, string nome, Vector3 pos)
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

        inimigoObj.transform.position = pos;
        inimigoObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

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

        return inimigoObj;
    }

    private static GameObject CriarObjetoObjetivo(int meta)
    {
        GameObject go = new GameObject("Objetivo_Fase");
        ObjetivoReliquias objetivo = go.AddComponent<ObjetivoReliquias>();
        SerializedObject so = new SerializedObject(objetivo);
        SerializedProperty propMeta = so.FindProperty("meta");
        if (propMeta != null)
            propMeta.intValue = meta;
        so.ApplyModifiedProperties();
        return go;
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

    private static void OrganizarCenaComSortify(
        Scene cena,
        GameObject camObj,
        GameObject luzObj,
        GameObject terrenoObj,
        GameObject playerObj,
        GameObject hudObj,
        List<GameObject> coletaveis,
        List<GameObject> inimigos,
        List<GameObject> armadilhas,
        GameObject sistemaObj,
        GameObject audioObj)
    {
        List<GameObject> huds = hudObj != null ? new List<GameObject> { hudObj } : null;
        List<GameObject> sistemas = sistemaObj != null ? new List<GameObject> { sistemaObj } : null;
        OrganizarCenaComSortify(cena, camObj, luzObj, terrenoObj, playerObj, huds, coletaveis, inimigos, armadilhas, sistemas, audioObj);
    }

    private static void OrganizarCenaComSortify(
        Scene cena,
        GameObject camObj,
        GameObject luzObj,
        GameObject terrenoObj,
        GameObject playerObj,
        List<GameObject> huds,
        List<GameObject> coletaveis,
        List<GameObject> inimigos,
        List<GameObject> armadilhas,
        List<GameObject> sistemas,
        GameObject audioObj)
    {
        string[] nomes = { "CAMERA", "LUZ", "TERRENO", "PLAYER", "HUD", "COLETAVEIS", "INIMIGOS", "ARMADILHAS", "SISTEMAS", "AUDIO" };
        string[] cores = { "#BF4040", "#BFBF40", "#40BF40", "#4CB5FF", "#BF4040", "#E6A817", "#D93838", "#E66C2C", "#6B7A8F", "#2FA89B" };

        Dictionary<string, Transform> raiz = new Dictionary<string, Transform>(StringComparer.Ordinal);
        for (int i = 0; i < nomes.Length; i++)
        {
            GameObject container = new GameObject(nomes[i]);
            container.transform.SetSiblingIndex(i);
            raiz[nomes[i]] = container.transform;
#if SORTIFY
            SortifyObjectManager.LoadDataIfNeeded(true);
            if (ColorUtility.TryParseHtmlString(cores[i], out Color cor))
                SortifyObjectManager.SaveColor(container, cor);
            if (nomes[i] != "HUD")
                SortifyObjectManager.SaveShowComponents(container, false);
#endif
        }

        ParentearSeNaoNulo(camObj, raiz["CAMERA"]);
        ParentearSeNaoNulo(luzObj, raiz["LUZ"]);
        ParentearSeNaoNulo(terrenoObj, raiz["TERRENO"]);
        ParentearSeNaoNulo(playerObj, raiz["PLAYER"]);
        ParentearSeNaoNulo(audioObj, raiz["AUDIO"]);

        ParentearLista(huds, raiz["HUD"]);
        ParentearLista(inimigos, raiz["INIMIGOS"]);
        ParentearLista(armadilhas, raiz["ARMADILHAS"]);
        ParentearLista(sistemas, raiz["SISTEMAS"]);

        if (coletaveis != null && coletaveis.Count > 0)
        {
            Transform subMoedas = CriarSubgrupo(raiz["COLETAVEIS"], "_MOEDAS", "#FFD700");
            Transform subReliquias = CriarSubgrupo(raiz["COLETAVEIS"], "_RELIQUIAS", "#C978FF");
            Transform subPocoes = CriarSubgrupo(raiz["COLETAVEIS"], "_POCOES", "#E64553");

            foreach (GameObject c in coletaveis)
            {
                if (c == null) continue;
                ItemColetavel ic = c.GetComponent<ItemColetavel>();
                Transform alvoPai = raiz["COLETAVEIS"];
                if (ic != null && ic.Item != null)
                {
                    alvoPai = ic.Item.Tipo switch
                    {
                        TipoItem.Moeda => subMoedas,
                        TipoItem.Reliquia => subReliquias,
                        TipoItem.Consumivel => subPocoes,
                        _ => raiz["COLETAVEIS"]
                    };
                }
                c.transform.SetParent(alvoPai, true);
            }
        }
    }

    private static Transform CriarSubgrupo(Transform pai, string nome, string hexCor)
    {
        GameObject sub = new GameObject(nome);
        sub.transform.SetParent(pai, false);
#if SORTIFY
        if (ColorUtility.TryParseHtmlString(hexCor, out Color cor))
            SortifyObjectManager.SaveColor(sub, cor);
        SortifyObjectManager.SaveShowComponents(sub, false);
#endif
        return sub.transform;
    }

    private static void ParentearSeNaoNulo(GameObject obj, Transform pai)
    {
        if (obj != null && pai != null)
            obj.transform.SetParent(pai, true);
    }

    private static void ParentearLista(List<GameObject> lista, Transform pai)
    {
        if (lista == null || pai == null)
            return;

        foreach (GameObject obj in lista)
        {
            if (obj != null)
                obj.transform.SetParent(pai, true);
        }
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
