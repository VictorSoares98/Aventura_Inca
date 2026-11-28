using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility that snaps floating scene objects back onto the Terrain
/// or ground surface with full Undo support and non-destructive safeguards.
/// </summary>
public static class AssentarObjetosNoSolo
{
    private enum CategoriaAlvo
    {
        Jogador,
        Inimigo,
        Coletavel,
        Armadilha,
        Cenario
    }

    private struct RegistroAlvo
    {
        public Transform Transform;
        public CategoriaAlvo Categoria;
    }

    private struct ContadoresColeta
    {
        public int Jogador;
        public int Inimigos;
        public int Coletaveis;
        public int Armadilhas;
        public int Cenario;

        public int Total => Jogador + Inimigos + Coletaveis + Armadilhas + Cenario;

        public void Incrementar(CategoriaAlvo categoria)
        {
            switch (categoria)
            {
                case CategoriaAlvo.Jogador:
                    Jogador++;
                    break;
                case CategoriaAlvo.Inimigo:
                    Inimigos++;
                    break;
                case CategoriaAlvo.Coletavel:
                    Coletaveis++;
                    break;
                case CategoriaAlvo.Armadilha:
                    Armadilhas++;
                    break;
                case CategoriaAlvo.Cenario:
                    Cenario++;
                    break;
            }
        }
    }

    private static readonly HashSet<string> NomesExcluidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CAMERA",
        "LUZ",
        "HUD",
        "SISTEMAS",
        "AUDIO",
        "_ESTRADA",
        "Objetos_Estrada",
        "Conexoes_Estrada",
        "estrada_principal"
    };

    private const float AlturaPadraoColetavel = 0.85f;
    private const float MargemTerrenoPiso = 1.5f;
    private const float OffsetOrigemRaycast = 500f;
    private const float DistanciaMaximaRaycast = 2000f;

    [MenuItem("Aventura Inca/9. Assentar Selecionados no Solo (1 Clique)", false, 9)]
    public static void AssentarSelecionados()
    {
        if (!TryObterCenaAtiva(out Scene cenaAtiva))
            return;

        Transform[] selecionados = Selection.transforms;
        if (selecionados == null || selecionados.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Nenhum Objeto Selecionado",
                "Selecione um ou mais objetos (ou pastas da Hierarchy) antes de usar esta opção.\n\n" +
                "Dica: Para ajustar toda a cena de uma vez, utilize o menu 'Aventura Inca -> 10. Assentar Todos os Objetos da Cena no Solo (1 Clique)'.",
                "OK");
            return;
        }

        List<Transform> raizesSelecao = FiltrarAncestraisSelecionados(selecionados);
        List<RegistroAlvo> alvos = new List<RegistroAlvo>();
        HashSet<Transform> visitados = new HashSet<Transform>();
        ContadoresColeta contadores = default;

        foreach (Transform t in raizesSelecao)
        {
            ColetarAlvosRecursivo(t, alvos, visitados, ref contadores);
        }

        if (alvos.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Nenhum Alvo Válido",
                "A seleção atual contém apenas objetos protegidos (Câmera, Luz, HUD, Terreno, Estradas EasyRoads3D) ou inativos.",
                "OK");
            return;
        }

        ExecutarAssentamento(cenaAtiva, alvos, "Assentar Selecionados no Solo",
            out int assentados, out int foraDoTerreno, out int jaNoLugar);

        string resumo =
            $"Assentamento concluído na cena '{cenaAtiva.name}'!\n\n" +
            $"✔ Objetos ajustados: {assentados}\n" +
            $"• Já estavam na altura correta: {jaNoLugar}\n" +
            $"⚠ Fora dos limites do terreno/solo (não movidos): {foraDoTerreno}\n\n" +
            "Você pode desfazer com Ctrl+Z ou salvar a cena com Ctrl+S.";

        EditorUtility.DisplayDialog("Assentar Selecionados", resumo, "OK");
    }

    [MenuItem("Aventura Inca/10. Assentar Todos os Objetos da Cena no Solo (1 Clique)", false, 10)]
    public static void AssentarTodosDaCena()
    {
        if (!TryObterCenaAtiva(out Scene cenaAtiva))
            return;

        List<RegistroAlvo> alvos = ColetarAlvosDaCena(cenaAtiva, out ContadoresColeta contadores);
        if (alvos.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Nenhum Objeto Encontrado",
                "Nenhum objeto ativo elegível foi encontrado para assentar nesta cena.",
                "OK");
            return;
        }

        string mensagemConfirmacao =
            $"Foram identificados {contadores.Total} objetos elegíveis na cena '{cenaAtiva.name}':\n\n" +
            $"• Jogador: {contadores.Jogador}\n" +
            $"• Inimigos: {contadores.Inimigos}\n" +
            $"• Coletáveis (+0.85m do solo): {contadores.Coletaveis}\n" +
            $"• Armadilhas: {contadores.Armadilhas}\n" +
            $"• Props de Cenário: {contadores.Cenario}\n\n" +
            "Observações de segurança:\n" +
            "- Terreno, Câmera, Luz, HUD, Sistemas, Áudio e malhas de estrada do EasyRoads3D são ignorados automaticamente.\n" +
            "- Toda a operação pode ser desfeita imediatamente com Ctrl+Z.";

        if (!EditorUtility.DisplayDialog("Confirmar Assentamento no Solo", mensagemConfirmacao, "Assentar no Solo", "Cancelar"))
            return;

        ExecutarAssentamento(cenaAtiva, alvos, "Assentar Todos os Objetos no Solo",
            out int assentados, out int foraDoTerreno, out int jaNoLugar);

        string resumo =
            $"Assentamento completo concluído em '{cenaAtiva.name}'!\n\n" +
            $"✔ Objetos ajustados: {assentados}\n" +
            $"• Já estavam na altura correta: {jaNoLugar}\n" +
            $"⚠ Fora dos limites do solo/terreno (preservados): {foraDoTerreno}\n\n" +
            (foraDoTerreno > 0
                ? "Verifique os avisos no Console para localizar os objetos fora do terreno.\n\n"
                : string.Empty) +
            "Lembre-se de conferir visualmente, usar Ctrl+Z se necessário e salvar a cena (Ctrl+S).";

        EditorUtility.DisplayDialog("Assentamento Concluído", resumo, "Excelente!");
    }

    private static bool TryObterCenaAtiva(out Scene cenaAtiva)
    {
        cenaAtiva = SceneManager.GetActiveScene();
        if (!cenaAtiva.IsValid() || !cenaAtiva.isLoaded)
        {
            EditorUtility.DisplayDialog("Aviso", "Nenhuma cena válida carregada no Editor.", "OK");
            return false;
        }

        return true;
    }

    private static List<Transform> FiltrarAncestraisSelecionados(Transform[] selecionados)
    {
        HashSet<Transform> conjuntoSelecao = new HashSet<Transform>(selecionados);
        List<Transform> resultado = new List<Transform>(selecionados.Length);

        foreach (Transform t in selecionados)
        {
            if (t == null)
                continue;

            bool temAncestralSelecionado = false;
            Transform atual = t.parent;
            while (atual != null)
            {
                if (conjuntoSelecao.Contains(atual))
                {
                    temAncestralSelecionado = true;
                    break;
                }

                atual = atual.parent;
            }

            if (!temAncestralSelecionado)
                resultado.Add(t);
        }

        return resultado;
    }

    private static List<RegistroAlvo> ColetarAlvosDaCena(Scene cena, out ContadoresColeta contadores)
    {
        List<RegistroAlvo> alvos = new List<RegistroAlvo>();
        HashSet<Transform> visitados = new HashSet<Transform>();
        contadores = default;

        GameObject[] raizes = cena.GetRootGameObjects();
        foreach (GameObject go in raizes)
        {
            if (go == null)
                continue;

            ColetarAlvosRecursivo(go.transform, alvos, visitados, ref contadores);
        }

        return alvos;
    }

    private static void ColetarAlvosRecursivo(
        Transform atual,
        List<RegistroAlvo> alvos,
        HashSet<Transform> visitados,
        ref ContadoresColeta contadores)
    {
        if (atual == null || visitados.Contains(atual))
            return;

        if (EstaExcluido(atual))
            return;

        if (TentarClassificarAlvoDireto(atual, out CategoriaAlvo categoria))
        {
            visitados.Add(atual);
            alvos.Add(new RegistroAlvo
            {
                Transform = atual,
                Categoria = categoria
            });
            contadores.Incrementar(categoria);
            return; // Never detach child sub-meshes from an object/prefab root
        }

        for (int i = 0; i < atual.childCount; i++)
        {
            ColetarAlvosRecursivo(atual.GetChild(i), alvos, visitados, ref contadores);
        }
    }

    private static bool EstaExcluido(Transform t)
    {
        if (t == null || !t.gameObject.activeInHierarchy)
            return true;

        if (NomesExcluidos.Contains(t.name))
            return true;

        return TemComponenteExcluido(t);
    }

    private static bool TemComponenteExcluido(Transform t)
    {
        Component[] componentes = t.GetComponents<Component>();
        for (int i = 0; i < componentes.Length; i++)
        {
            Component comp = componentes[i];
            if (comp == null)
                continue;

            if (comp is Terrain ||
                comp is Camera ||
                comp is Light ||
                comp is Canvas ||
                comp is RectTransform ||
                comp is AudioListener ||
                comp is EventSystem)
            {
                return true;
            }

            if (EhComponenteEasyRoads(comp))
                return true;
        }

        return false;
    }

    private static bool EhComponenteEasyRoads(Component comp)
    {
        string nomeTipo = comp.GetType().Name;
        return nomeTipo.StartsWith("ERModular", StringComparison.Ordinal) ||
               nomeTipo.StartsWith("ERCrossing", StringComparison.Ordinal) ||
               nomeTipo.StartsWith("ERConnection", StringComparison.Ordinal);
    }

    private static bool TentarClassificarAlvoDireto(Transform t, out CategoriaAlvo categoria)
    {
        if (t.GetComponent<ControleJogador>() != null || t.CompareTag("Player"))
        {
            categoria = CategoriaAlvo.Jogador;
            return true;
        }

        if (t.GetComponent<InimigoPerseguidor>() != null || t.GetComponent<NavMeshAgent>() != null)
        {
            categoria = CategoriaAlvo.Inimigo;
            return true;
        }

        if (t.GetComponent<ItemColetavel>() != null)
        {
            categoria = CategoriaAlvo.Coletavel;
            return true;
        }

        if (t.GetComponent<Armadilha>() != null)
        {
            categoria = CategoriaAlvo.Armadilha;
            return true;
        }

        if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && TemVisualOuColisorNosFilhos(t))
        {
            categoria = CategoriaAlvo.Cenario;
            return true;
        }

        if (TemVisualOuColisorDireto(t))
        {
            categoria = CategoriaAlvo.Cenario;
            return true;
        }

        categoria = default;
        return false;
    }

    private static bool TemVisualOuColisorDireto(Transform t)
    {
        Renderer[] renderers = t.GetComponents<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (EhRendererSolido(renderers[i]))
                return true;
        }

        Collider[] colliders = t.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null && !(col is TerrainCollider))
                return true;
        }

        return false;
    }

    private static bool TemVisualOuColisorNosFilhos(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (EhRendererSolido(renderers[i]))
                return true;
        }

        Collider[] colliders = t.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null && !(col is TerrainCollider))
                return true;
        }

        return false;
    }

    private static bool EhRendererSolido(Renderer rend)
    {
        if (rend == null || !rend.enabled)
            return false;

        return !(rend is ParticleSystemRenderer) &&
               !(rend is TrailRenderer) &&
               !(rend is LineRenderer);
    }

    private static void ExecutarAssentamento(
        Scene cenaAtiva,
        List<RegistroAlvo> alvos,
        string nomeUndo,
        out int assentados,
        out int foraDoTerreno,
        out int jaNoLugar)
    {
        assentados = 0;
        foraDoTerreno = 0;
        jaNoLugar = 0;

        HashSet<Transform> alvosLote = new HashSet<Transform>();
        for (int i = 0; i < alvos.Count; i++)
        {
            if (alvos[i].Transform != null)
                alvosLote.Add(alvos[i].Transform);
        }

        Terrain[] terrenosAtivos = Terrain.activeTerrains;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(nomeUndo);

        for (int i = 0; i < alvos.Count; i++)
        {
            Transform alvo = alvos[i].Transform;
            if (alvo == null)
                continue;

            if (!TentarObterAlturaSolo(alvo, alvosLote, terrenosAtivos, out float yChao))
            {
                foraDoTerreno++;
                Debug.LogWarning(
                    $"[AssentarObjetosNoSolo] '{alvo.name}' está fora dos limites X/Z do terreno ou sem solo abaixo e não foi movido.",
                    alvo.gameObject);
                continue;
            }

            float offsetBase = CalcularOffsetBase(alvo);
            float novoY = yChao + offsetBase;

            if (Mathf.Abs(alvo.position.y - novoY) < 0.001f)
            {
                jaNoLugar++;
                continue;
            }

            Undo.RecordObject(alvo, "Assentar no Solo");
            Vector3 novaPos = alvo.position;
            novaPos.y = novoY;
            alvo.position = novaPos;
            EditorUtility.SetDirty(alvo);
            assentados++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (assentados > 0)
            EditorSceneManager.MarkSceneDirty(cenaAtiva);

        Debug.Log(
            $"[AssentarObjetosNoSolo] '{cenaAtiva.name}': {assentados} ajustados, {jaNoLugar} já no solo, {foraDoTerreno} fora do terreno.");
    }

    private static bool TentarObterAlturaSolo(
        Transform alvo,
        HashSet<Transform> alvosLote,
        Terrain[] terrenosAtivos,
        out float yChao)
    {
        Vector3 pos = alvo.position;
        bool temTerreno = TentarAmostrarTerreno(pos, terrenosAtivos, out float yTerreno);

        float baseOrigemY = temTerreno ? Mathf.Max(pos.y, yTerreno) : pos.y;
        Vector3 origemRay = new Vector3(pos.x, baseOrigemY + OffsetOrigemRaycast, pos.z);
        RaycastHit[] hits = Physics.RaycastAll(origemRay, Vector3.down, DistanciaMaximaRaycast, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        bool encontrouPisoValido = false;
        float melhorPisoY = float.NegativeInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (!EhHitSoloValido(hit, alvosLote))
                continue;

            if (temTerreno)
            {
                // Keep conformed pavements/platforms right above terrain, ignore floating structures high above
                if (hit.point.y > yTerreno && hit.point.y <= yTerreno + MargemTerrenoPiso)
                {
                    if (!encontrouPisoValido || hit.point.y > melhorPisoY)
                    {
                        melhorPisoY = hit.point.y;
                        encontrouPisoValido = true;
                    }
                }
            }
            else
            {
                if (hit.point.y <= pos.y + 50f && (!encontrouPisoValido || hit.point.y > melhorPisoY))
                {
                    melhorPisoY = hit.point.y;
                    encontrouPisoValido = true;
                }
            }
        }

        if (temTerreno)
        {
            yChao = encontrouPisoValido ? melhorPisoY : yTerreno;
            return true;
        }

        if (encontrouPisoValido)
        {
            yChao = melhorPisoY;
            return true;
        }

        yChao = pos.y;
        return false;
    }

    private static bool TentarAmostrarTerreno(Vector3 pos, Terrain[] terrenosAtivos, out float yTerreno)
    {
        yTerreno = float.NegativeInfinity;
        bool dentroDeTerreno = false;

        if (terrenosAtivos == null)
            return false;

        for (int i = 0; i < terrenosAtivos.Length; i++)
        {
            Terrain terreno = terrenosAtivos[i];
            if (terreno == null || !terreno.gameObject.activeInHierarchy || terreno.terrainData == null)
                continue;

            Vector3 tPos = terreno.transform.position;
            Vector3 tSize = terreno.terrainData.size;

            bool dentroX = pos.x >= tPos.x && pos.x <= tPos.x + tSize.x;
            bool dentroZ = pos.z >= tPos.z && pos.z <= tPos.z + tSize.z;

            if (!dentroX || !dentroZ)
                continue;

            float altura = terreno.SampleHeight(pos) + tPos.y;
            if (!dentroDeTerreno || altura > yTerreno)
            {
                yTerreno = altura;
                dentroDeTerreno = true;
            }
        }

        return dentroDeTerreno;
    }

    private static bool EhHitSoloValido(RaycastHit hit, HashSet<Transform> alvosLote)
    {
        Collider col = hit.collider;
        if (col == null || col.isTrigger)
            return false;

        Transform hitTransform = col.transform;
        if (PertenceAAlgumAlvo(hitTransform, alvosLote))
            return false;

        if (col.GetComponentInParent<ControleJogador>() != null ||
            col.GetComponentInParent<InimigoPerseguidor>() != null ||
            col.GetComponentInParent<ItemColetavel>() != null ||
            col.GetComponentInParent<Armadilha>() != null)
        {
            return false;
        }

        if (PossuiEasyRoadsNaHierarquia(hitTransform))
            return false;

        return true;
    }

    private static bool PertenceAAlgumAlvo(Transform t, HashSet<Transform> alvosLote)
    {
        Transform atual = t;
        while (atual != null)
        {
            if (alvosLote.Contains(atual))
                return true;

            atual = atual.parent;
        }

        return false;
    }

    private static bool PossuiEasyRoadsNaHierarquia(Transform t)
    {
        Transform atual = t;
        while (atual != null)
        {
            if (NomesExcluidos.Contains(atual.name) &&
                (atual.name.Equals("_ESTRADA", StringComparison.OrdinalIgnoreCase) ||
                 atual.name.Equals("Objetos_Estrada", StringComparison.OrdinalIgnoreCase) ||
                 atual.name.Equals("Conexoes_Estrada", StringComparison.OrdinalIgnoreCase) ||
                 atual.name.Equals("estrada_principal", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            Component[] componentes = atual.GetComponents<Component>();
            for (int i = 0; i < componentes.Length; i++)
            {
                Component comp = componentes[i];
                if (comp != null && EhComponenteEasyRoads(comp))
                    return true;
            }

            atual = atual.parent;
        }

        return false;
    }

    private static float CalcularOffsetBase(Transform alvo)
    {
        ItemColetavel coletavel = alvo.GetComponent<ItemColetavel>();
        if (coletavel != null)
        {
            SerializedObject so = new SerializedObject(coletavel);
            SerializedProperty propAltura = so.FindProperty("alturaAcimaDoSolo");
            if (propAltura != null && propAltura.propertyType == SerializedPropertyType.Float)
                return Mathf.Max(0.2f, propAltura.floatValue);

            return AlturaPadraoColetavel;
        }

        ControleJogador jogador = alvo.GetComponent<ControleJogador>();
        if (jogador != null)
        {
            CharacterController cc = alvo.GetComponent<CharacterController>();
            if (cc != null && cc.enabled && cc.bounds.size != Vector3.zero)
                return alvo.position.y - cc.bounds.min.y + 0.02f;

            return 0.02f;
        }

        Bounds boundsCombinado = default;
        bool possuiBounds = false;

        Renderer[] renderers = alvo.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            if (!EhRendererSolido(rend) || rend.bounds.size == Vector3.zero)
                continue;

            if (!possuiBounds)
            {
                boundsCombinado = rend.bounds;
                possuiBounds = true;
            }
            else
            {
                boundsCombinado.Encapsulate(rend.bounds);
            }
        }

        if (possuiBounds)
            return alvo.position.y - boundsCombinado.min.y;

        Collider[] colliders = alvo.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col is TerrainCollider || col.isTrigger || col.bounds.size == Vector3.zero)
                continue;

            if (!possuiBounds)
            {
                boundsCombinado = col.bounds;
                possuiBounds = true;
            }
            else
            {
                boundsCombinado.Encapsulate(col.bounds);
            }
        }

        if (possuiBounds)
            return alvo.position.y - boundsCombinado.min.y;

        return 0f;
    }
}
