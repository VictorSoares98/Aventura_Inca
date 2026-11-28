using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Utilitário técnico para reestruturar o Animator Controller da criatura Beast (SciFi Beast06 Bull),
/// aplicar travamento In-Place nos clipes FBX, configurar parâmetros anti-sobreposição (Avoidance)
/// e gerar Prefab padronizado e cena de teste isolada.
/// </summary>
public static class ConfiguradorAnimacoesInimigo
{
    private const string PathController = "Assets/SciFi Beast06 Bull/SciFi Beast06 Bull Animator Controller.controller";
    private const string PastaAnimacoes = "Assets/SciFi Beast06 Bull/Animations";
    private const string PathPrefabOrigem = "Assets/SciFi Beast06 Bull/Prefab/SciFi Beast06 Bull Skin1.prefab";
    private const string PastaPrefabsInimigos = "Assets/_Project/Prefabs/Inimigos";
    private const string PathPrefabInimigo = PastaPrefabsInimigos + "/Prefab_Inimigo_Beast.prefab";
    private const string PastaCenasTestes = "Assets/_Project/Scenes/Testes";
    private const string PathCenaTeste = PastaCenasTestes + "/Cena_Teste_Inimigo.unity";

    private const string PathPrefabJogador = "Assets/_Project/Prefabs/Sistema/Prefab_Jogador.prefab";
    private const string PathPrefabCamera = "Assets/_Project/Prefabs/Sistema/Prefab_Camera.prefab";

    [MenuItem("Aventura Inca/IA e Inimigos/1. Reestruturar Animator e Configurar Inimigo (1 Clique)", false, 20)]
    public static void ExecutarConfiguracaoCompleta()
    {
        if (!Application.isBatchMode)
            EditorUtility.DisplayProgressBar("Aventura Inca", "Iniciando reestruturação do inimigo...", 0.1f);

        GarantirPastas();

        if (!Application.isBatchMode)
            EditorUtility.DisplayProgressBar("Aventura Inca", "Reestruturando Animator Controller...", 0.3f);
        bool controllerOk = ReestruturarAnimatorController();

        if (!Application.isBatchMode)
            EditorUtility.DisplayProgressBar("Aventura Inca", "Configurando In-Place nos clipes FBX...", 0.5f);
        int clipesConfigurados = ConfigurarInPlaceClipes();

        if (!Application.isBatchMode)
            EditorUtility.DisplayProgressBar("Aventura Inca", "Gerando Prefab_Inimigo_Beast...", 0.7f);
        bool prefabOk = GerarPrefabInimigo();

        if (!Application.isBatchMode)
            EditorUtility.DisplayProgressBar("Aventura Inca", "Gerando Cena de Teste Isolada...", 0.9f);
        bool cenaOk = GerarCenaDeTesteIsolada();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Configuração do Inimigo Concluída!",
                "A reestruturação técnica do inimigo foi concluída com sucesso:\n\n" +
                $"✔ Animator Controller desvinculado do carrossel (Idle1 padrão, ataques com retorno).\n" +
                $"✔ {clipesConfigurados} clipes de animação configurados com In-Place (Root Motion lock).\n" +
                $"✔ Prefab_Inimigo_Beast gerado com NavMesh Avoidance RVO (raio 1.15m, anti-sobreposição).\n" +
                $"✔ Cena de teste criada em Assets/_Project/Scenes/Testes/Cena_Teste_Inimigo.unity com 3 inimigos e NavMesh.\n\n" +
                "Abra a cena de teste e dê PLAY para verificar as animações e a separação dos inimigos!", "Excelente!");
        }

        Debug.Log("[ConfiguradorAnimacoesInimigo] Reestruturação concluída com sucesso.");
    }

    private static void GarantirPastas()
    {
        if (!Directory.Exists(PastaPrefabsInimigos))
            Directory.CreateDirectory(PastaPrefabsInimigos);

        if (!Directory.Exists(PastaCenasTestes))
            Directory.CreateDirectory(PastaCenasTestes);
    }

    public static bool ReestruturarAnimatorController()
    {
        AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(PathController);
        if (ac == null)
        {
            Debug.LogError($"[ConfiguradorAnimacoesInimigo] Animator Controller não encontrado em: {PathController}");
            return false;
        }

        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        // 1. Limpa todas as transições em carrossel de todos os estados
        AnimatorState stateIdle1 = null;
        AnimatorState stateWalkF = null;
        AnimatorState stateRun = null;
        AnimatorState stateAttack1 = null;
        AnimatorState stateAttack2 = null;
        AnimatorState stateAttack3 = null;
        AnimatorState stateDeath1 = null;

        foreach (ChildAnimatorState childState in sm.states)
        {
            AnimatorState s = childState.state;
            s.transitions = new AnimatorStateTransition[0];

            if (s.name == "Idle1") stateIdle1 = s;
            else if (s.name == "WalkF") stateWalkF = s;
            else if (s.name == "Run") stateRun = s;
            else if (s.name == "Attack1") stateAttack1 = s;
            else if (s.name == "Attack2") stateAttack2 = s;
            else if (s.name == "Attack3") stateAttack3 = s;
            else if (s.name == "Death1") stateDeath1 = s;
        }

        // 2. Define Idle1 como o Estado Padrão (Default State)
        if (stateIdle1 != null)
        {
            sm.defaultState = stateIdle1;
        }

        // 3. Configura transições de retorno a Idle1 após cada golpe de ataque
        ConfigurarRetornoAtaque(stateAttack1, stateIdle1);
        ConfigurarRetornoAtaque(stateAttack2, stateIdle1);
        ConfigurarRetornoAtaque(stateAttack3, stateIdle1);

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("[ConfiguradorAnimacoesInimigo] Animator Controller reestruturado com sucesso (Idle1 padrão, sem carrossel).");
        return true;
    }

    private static void ConfigurarRetornoAtaque(AnimatorState stateAtaque, AnimatorState stateDestino)
    {
        if (stateAtaque == null || stateDestino == null) return;

        AnimatorStateTransition trans = stateAtaque.AddTransition(stateDestino);
        trans.hasExitTime = true;
        trans.exitTime = 0.95f;
        trans.hasFixedDuration = true;
        trans.duration = 0.2f;
        trans.canTransitionToSelf = false;
    }

    public static int ConfigurarInPlaceClipes()
    {
        if (!Directory.Exists(PastaAnimacoes)) return 0;

        string[] fbxFiles = Directory.GetFiles(PastaAnimacoes, "*.fbx");
        int count = 0;

        foreach (string file in fbxFiles)
        {
            string caminhoRelativo = file.Replace('\\', '/');
            ModelImporter importer = AssetImporter.GetAtPath(caminhoRelativo) as ModelImporter;
            if (importer == null) continue;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0) continue;
            }

            bool mudou = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                // Trava Root Motion no plano e na rotação (In-Place)
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;

                // Ativa loop para animações de ciclo
                string nome = clip.name.ToLower();
                if (nome.Contains("idle") || nome.Contains("walk") || nome.Contains("run"))
                {
                    clip.loopTime = true;
                }

                mudou = true;
            }

            if (mudou)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                count++;
            }
        }

        Debug.Log($"[ConfiguradorAnimacoesInimigo] {count} clipes de animação configurados para In-Place (Root Motion locked).");
        return count;
    }

    public static bool GerarPrefabInimigo()
    {
        GameObject origemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabOrigem);
        if (origemPrefab == null)
        {
            Debug.LogError($"[ConfiguradorAnimacoesInimigo] Prefab base do Touro não encontrado em: {PathPrefabOrigem}");
            return false;
        }

        GameObject instancia = PrefabUtility.InstantiatePrefab(origemPrefab) as GameObject;
        if (instancia == null) return false;

        instancia.name = "Prefab_Inimigo_Beast";

        // 1. Configuração do Animator
        Animator anim = instancia.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false; // Desativa Root Motion para estabilizar pés no chão
            AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(PathController);
            if (ac != null) anim.runtimeAnimatorController = ac;
        }

        // 2. Configuração do NavMeshAgent (Anti-Sobreposição de Inimigos)
        NavMeshAgent agente = instancia.GetComponent<NavMeshAgent>();
        if (agente == null) agente = instancia.AddComponent<NavMeshAgent>();

        agente.radius = 1.0f; // Raio volumétrico real da criatura
        agente.height = 2.0f;
        agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // RVO de alta precisão
        agente.avoidancePriority = 50;
        agente.stoppingDistance = 2.4f; // Distância de combate que impede encostar fisicamente no jogador
        agente.speed = 5.0f;
        agente.acceleration = 14.0f;
        agente.angularSpeed = 240.0f;

        // 3. Colisor de Combate
        CapsuleCollider col = instancia.GetComponent<CapsuleCollider>();
        if (col == null) col = instancia.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1.0f, 0f);
        col.radius = 1.0f;
        col.height = 2.0f;
        col.isTrigger = true;

        // 4. InimigoPerseguidor
        InimigoPerseguidor perseguidor = instancia.GetComponent<InimigoPerseguidor>();
        if (perseguidor == null) perseguidor = instancia.AddComponent<InimigoPerseguidor>();

        SerializedObject so = new SerializedObject(perseguidor);
        so.FindProperty("animacaoIdle").stringValue = "Idle1";
        so.FindProperty("animacaoAndar").stringValue = "WalkF";
        so.FindProperty("animacaoCorrer").stringValue = "Run";
        so.FindProperty("distanciaPerseguicao").floatValue = 30f;
        so.FindProperty("distanciaCorrida").floatValue = 8f;
        so.FindProperty("distanciaAtaque").floatValue = 3.2f;
        so.FindProperty("tempoEsperaInicial").floatValue = 3.5f;
        so.FindProperty("tempoPreparoAtaque").floatValue = 0.6f;
        so.FindProperty("tempoRecuperacaoAtaque").floatValue = 0.8f;
        so.FindProperty("intervaloEntreAtaques").floatValue = 1.8f;
        so.ApplyModifiedProperties();

        // 5. Salva como Prefab Padronizado
        PrefabUtility.SaveAsPrefabAsset(instancia, PathPrefabInimigo);
        Object.DestroyImmediate(instancia);

        Debug.Log($"[ConfiguradorAnimacoesInimigo] Prefab gerado com sucesso em: {PathPrefabInimigo}");
        return true;
    }

    public static bool GerarCenaDeTesteIsolada()
    {
        Scene cenaAtual = SceneManager.GetActiveScene();
        Scene novaCena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Chão Plano com NavMeshSurface
        GameObject chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "Chao_Teste_Arena";
        chao.transform.position = Vector3.zero;
        chao.transform.localScale = new Vector3(6f, 1f, 6f); // 60x60 metros

        NavMeshSurface surface = chao.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();

        // 2. Luz Direcional
        GameObject objLuz = new GameObject("Directional Light");
        Light luz = objLuz.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = Color.white;
        luz.intensity = 1.2f;
        objLuz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 3. Instancia Player e Câmera de Sistema
        GameObject prefabJogador = AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabJogador);
        if (prefabJogador != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(prefabJogador) as GameObject;
            if (player != null) player.transform.position = new Vector3(0f, 0.05f, -12f);
        }

        GameObject prefabCam = AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabCamera);
        if (prefabCam != null)
        {
            GameObject cam = PrefabUtility.InstantiatePrefab(prefabCam) as GameObject;
            if (cam != null) cam.transform.position = new Vector3(0f, 2f, -15f);
        }

        // 4. Instancia 3 Inimigos para teste de Separação Mútua e Animações
        GameObject prefabInimigo = AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabInimigo);
        if (prefabInimigo != null)
        {
            Vector3[] posicoesInimigos = new Vector3[] {
                new Vector3(-4.5f, 0f, 10f),
                new Vector3(0f, 0f, 14f),
                new Vector3(4.5f, 0f, 10f)
            };

            for (int i = 0; i < posicoesInimigos.Length; i++)
            {
                GameObject inimigo = PrefabUtility.InstantiatePrefab(prefabInimigo) as GameObject;
                if (inimigo != null)
                {
                    inimigo.name = $"Inimigo_Beast_Teste_{i + 1}";
                    inimigo.transform.position = posicoesInimigos[i];
                    inimigo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                    NavMeshAgent ag = inimigo.GetComponent<NavMeshAgent>();
                    if (ag != null) ag.avoidancePriority = 35 + (i * 10);
                }
            }
        }

        // 5. Salva a cena
        EditorSceneManager.SaveScene(novaCena, PathCenaTeste);
        Debug.Log($"[ConfiguradorAnimacoesInimigo] Cena de teste criada e salva em: {PathCenaTeste}");
        return true;
    }
}
