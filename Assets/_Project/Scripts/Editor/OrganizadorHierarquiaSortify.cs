using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if SORTIFY
using Sortify;
#endif

/// <summary>
/// Organizes the active scene Hierarchy into standardized root containers
/// and applies Sortify colors and component-icon visibility settings.
/// </summary>
public static class OrganizadorHierarquiaSortify
{
    private static readonly (string Nome, string HexCor, bool OcultarComponentes)[] GruposRaiz =
    {
        ("CAMERA", "#BF4040", true),
        ("LUZ", "#BFBF40", true),
        ("TERRENO", "#40BF40", true),
        ("PLAYER", "#4CB5FF", true),
        ("HUD", "#BF4040", false),
        ("COLETAVEIS", "#E6A817", true),
        ("INIMIGOS", "#D93838", true),
        ("ARMADILHAS", "#E66C2C", true),
        ("SISTEMAS", "#6B7A8F", true),
        ("AUDIO", "#2FA89B", true)
    };

    private static readonly (string Nome, string HexCor)[] SubgruposColetaveis =
    {
        ("_MOEDAS", "#FFD700"),
        ("_RELIQUIAS", "#C978FF"),
        ("_POCOES", "#E64553")
    };

    [MenuItem("Aventura Inca/7. Organizar Hierarquia e Aplicar Sortify (1 Clique)", false, 7)]
    public static void OrganizarCenaCompleta()
    {
        Scene cenaAtiva = SceneManager.GetActiveScene();
        if (!cenaAtiva.IsValid() || !cenaAtiva.isLoaded)
        {
            EditorUtility.DisplayDialog("Aviso", "Nenhuma cena válida carregada no Editor.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Organizar Hierarquia e Aplicar Sortify");

        // 1) Garantir os 10 grupos raiz na ordem fixa 0..9
        Dictionary<string, Transform> containersRaiz = new Dictionary<string, Transform>(StringComparer.Ordinal);
        for (int i = 0; i < GruposRaiz.Length; i++)
        {
            Transform t = ObterOuCriarRaiz(cenaAtiva, GruposRaiz[i].Nome);
            t.SetSiblingIndex(i);
            containersRaiz[GruposRaiz[i].Nome] = t;
        }

        // 2) Garantir subgrupos dentro de COLETAVEIS
        Transform raizColetaveis = containersRaiz["COLETAVEIS"];
        Dictionary<string, Transform> subColetaveis = new Dictionary<string, Transform>(StringComparer.Ordinal);
        for (int i = 0; i < SubgruposColetaveis.Length; i++)
        {
            Transform sub = ObterOuCriarFilho(raizColetaveis, SubgruposColetaveis[i].Nome);
            sub.SetSiblingIndex(i);
            subColetaveis[SubgruposColetaveis[i].Nome] = sub;
        }

        // 3) Classificar e agrupar objetos soltos na raiz da cena
        GameObject[] objetosRaiz = cenaAtiva.GetRootGameObjects();
        HashSet<Transform> containersProtegidos = new HashSet<Transform>(containersRaiz.Values);

        int movColetaveis = 0;
        int movInimigos = 0;
        int movArmadilhas = 0;
        int movSistemas = 0;
        int movAudio = 0;

        foreach (GameObject go in objetosRaiz)
        {
            if (go == null || containersProtegidos.Contains(go.transform))
                continue;

            Transform destino = ClassificarObjeto(go, containersRaiz, subColetaveis,
                ref movColetaveis, ref movInimigos, ref movArmadilhas, ref movSistemas, ref movAudio);

            if (destino != null && go.transform.parent != destino)
            {
                Undo.SetTransformParent(go.transform, destino, true, "Organizar Hierarquia");
            }
        }

        // Reaplicar ordem 0..9 dos grupos raiz após possíveis movimentações
        for (int i = 0; i < GruposRaiz.Length; i++)
        {
            containersRaiz[GruposRaiz[i].Nome].SetSiblingIndex(i);
        }

        // 4) Aplicar paleta e visibilidade do Sortify
        AplicarEstilosSortify(containersRaiz, subColetaveis);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(cenaAtiva);
        EditorApplication.RepaintHierarchyWindow();

        string resumo =
            $"Hierarquia organizada e Sortify aplicado na cena '{cenaAtiva.name}'!\n\n" +
            $"✔ Coletáveis agrupados: {movColetaveis}\n" +
            $"✔ Inimigos agrupados: {movInimigos}\n" +
            $"✔ Armadilhas agrupadas: {movArmadilhas}\n" +
            $"✔ Sistemas agrupados: {movSistemas}\n" +
            $"✔ Áudio agrupado: {movAudio}\n\n" +
            "Lembre-se de salvar a cena (Ctrl+S).";

        EditorUtility.DisplayDialog("Hierarquia Organizada!", resumo, "Excelente!");
        Debug.Log($"[OrganizadorHierarquiaSortify] Cena '{cenaAtiva.name}' organizada com sucesso.");
    }

    /// <summary>
    /// Returns an existing root container (or optional subgroup) or creates it cleanly at origin.
    /// </summary>
    public static Transform ObterOuCriarContainer(string nomeRaiz, string nomeSubgrupo = null)
    {
        Scene cenaAtiva = SceneManager.GetActiveScene();
        Transform raiz = ObterOuCriarRaiz(cenaAtiva, nomeRaiz);

        if (string.IsNullOrEmpty(nomeSubgrupo))
            return raiz;

        return ObterOuCriarFilho(raiz, nomeSubgrupo);
    }

    private static Transform ClassificarObjeto(
        GameObject go,
        Dictionary<string, Transform> raiz,
        Dictionary<string, Transform> subColetaveis,
        ref int movColetaveis,
        ref int movInimigos,
        ref int movArmadilhas,
        ref int movSistemas,
        ref int movAudio)
    {
        string nome = go.name;

        ItemColetavel coletavel = go.GetComponent<ItemColetavel>();
        if (coletavel != null || nome.IndexOf("Coletavel", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            movColetaveis++;
            if (coletavel != null && coletavel.Item != null)
            {
                return coletavel.Item.Tipo switch
                {
                    TipoItem.Moeda => subColetaveis["_MOEDAS"],
                    TipoItem.Reliquia => subColetaveis["_RELIQUIAS"],
                    TipoItem.Consumivel => subColetaveis["_POCOES"],
                    _ => raiz["COLETAVEIS"]
                };
            }

            if (nome.IndexOf("Moeda", StringComparison.OrdinalIgnoreCase) >= 0)
                return subColetaveis["_MOEDAS"];
            if (nome.IndexOf("Reliquia", StringComparison.OrdinalIgnoreCase) >= 0)
                return subColetaveis["_RELIQUIAS"];
            if (nome.IndexOf("Pocao", StringComparison.OrdinalIgnoreCase) >= 0)
                return subColetaveis["_POCOES"];

            return raiz["COLETAVEIS"];
        }

        if (go.GetComponent<InimigoPerseguidor>() != null ||
            nome.IndexOf("Inimigo", StringComparison.OrdinalIgnoreCase) >= 0 ||
            nome.IndexOf("SciFi Beast", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            movInimigos++;
            return raiz["INIMIGOS"];
        }

        if (go.GetComponent<Armadilha>() != null ||
            string.Equals(nome, "AreaDeDano", StringComparison.OrdinalIgnoreCase))
        {
            movArmadilhas++;
            return raiz["ARMADILHAS"];
        }

        if (go.GetComponent<EventSystem>() != null ||
            go.GetComponent<ObjetivoReliquias>() != null ||
            string.Equals(nome, "EventSystem", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(nome, "Objetivo_Fase", StringComparison.OrdinalIgnoreCase))
        {
            movSistemas++;
            return raiz["SISTEMAS"];
        }

        if (go.GetComponentInChildren<AudioSource>() != null ||
            string.Equals(nome, "BackgroundMusic", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(nome, "Audios", StringComparison.OrdinalIgnoreCase))
        {
            movAudio++;
            return raiz["AUDIO"];
        }

        return null;
    }

    private static Transform ObterOuCriarRaiz(Scene cena, string nome)
    {
        foreach (GameObject root in cena.GetRootGameObjects())
        {
            if (string.Equals(root.name, nome, StringComparison.Ordinal))
                return root.transform;
        }

        GameObject novo = new GameObject(nome);
        novo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        novo.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(novo, $"Criar Grupo {nome}");
        return novo.transform;
    }

    private static Transform ObterOuCriarFilho(Transform pai, string nome)
    {
        Transform existente = pai.Find(nome);
        if (existente != null)
            return existente;

        GameObject novo = new GameObject(nome);
        novo.transform.SetParent(pai, false);
        novo.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        novo.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(novo, $"Criar Subgrupo {nome}");
        return novo.transform;
    }

    private static void AplicarEstilosSortify(
        Dictionary<string, Transform> raiz,
        Dictionary<string, Transform> subColetaveis)
    {
#if SORTIFY
        SortifyObjectManager.LoadDataIfNeeded(true);

        foreach (var grupo in GruposRaiz)
        {
            GameObject go = raiz[grupo.Nome].gameObject;
            AplicarCor(go, grupo.HexCor);
            if (grupo.OcultarComponentes)
                SortifyObjectManager.SaveShowComponents(go, false);
        }

        foreach (var sub in SubgruposColetaveis)
        {
            GameObject go = subColetaveis[sub.Nome].gameObject;
            AplicarCor(go, sub.HexCor);
            SortifyObjectManager.SaveShowComponents(go, false);
        }

        // Preservar cores dos subgrupos internos de TERRENO quando existirem
        Transform terreno = raiz["TERRENO"];
        Transform conteudoTerreno = terreno.Find("CONTEUDO TERRENO");
        if (conteudoTerreno != null)
        {
            AplicarCor(conteudoTerreno.gameObject, "#008080");
            SortifyObjectManager.SaveShowComponents(conteudoTerreno.gameObject, false);

            string[] subTerreno = { "POSTE DE LUZ", "_ESTRADA", "_QUEBRA MOLA", "_CONE", "_GRADE" };
            foreach (string nomeSub in subTerreno)
            {
                Transform filho = conteudoTerreno.Find(nomeSub);
                if (filho != null)
                {
                    AplicarCor(filho.gameObject, "#8000FF");
                    SortifyObjectManager.SaveShowComponents(filho.gameObject, false);
                }
            }
        }
#endif
    }

#if SORTIFY
    private static void AplicarCor(GameObject go, string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color cor))
        {
            SortifyObjectManager.SaveColor(go, cor);
        }
    }
#endif
}
