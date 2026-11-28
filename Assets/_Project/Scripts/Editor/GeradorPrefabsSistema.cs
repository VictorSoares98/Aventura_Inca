using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Extracts decoupled, reusable system Prefabs (Player, HUD+Inventory, Camera)
/// from the active scene into Assets/_Project/Prefabs/Sistema.
/// </summary>
public static class GeradorPrefabsSistema
{
    private const string PastaSistema = "Assets/_Project/Prefabs/Sistema";

    [MenuItem("Aventura Inca/8. Gerar Prefabs de Sistema (Player, HUD, Camera)", false, 8)]
    public static void GerarPrefabsDeSistema()
    {
        GarantirPasta(PastaSistema);

        bool criouJogador = GerarPrefabJogador();
        bool criouHud = GerarPrefabHUD();
        bool criouCamera = GerarPrefabCamera();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string resumo =
            "Prefabs de Sistema gerados em Assets/_Project/Prefabs/Sistema/:\n\n" +
            $"{(criouJogador ? "✔" : "✖")} Prefab_Jogador.prefab\n" +
            $"{(criouHud ? "✔" : "✖")} Prefab_HUD_Sistema.prefab\n" +
            $"{(criouCamera ? "✔" : "✖")} Prefab_Camera.prefab\n\n" +
            "Como testar em uma cena nova:\n" +
            "1. Remova a Main Camera padrão vazia da nova cena;\n" +
            "2. Arraste os 3 Prefabs para a Hierarchy;\n" +
            "3. Dê PLAY (nenhuma referência cruzada manual é necessária).";

        EditorUtility.DisplayDialog("Prefabs de Sistema", resumo, "OK");
        Debug.Log("[GeradorPrefabsSistema] Processo concluído.");
    }

    private static bool GerarPrefabJogador()
    {
        GameObject origem = GameObject.FindWithTag("Player") ?? GameObject.Find("Ch18_nonPBR");
        if (origem == null)
        {
            Debug.LogWarning("[GeradorPrefabsSistema] Jogador não encontrado na cena atual.");
            return false;
        }

        GameObject clone = Object.Instantiate(origem);
        clone.name = "Prefab_Jogador";
        clone.tag = "Player";
        clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        ControleJogador controle = clone.GetComponent<ControleJogador>();
        if (controle != null)
        {
            SerializedObject soControle = new SerializedObject(controle);
            SerializedProperty propCam = soControle.FindProperty("transformCamera");
            if (propCam != null)
                propCam.objectReferenceValue = null;
            soControle.ApplyModifiedProperties();
        }

        VidaPersonagem vida = clone.GetComponent<VidaPersonagem>();
        if (vida != null)
        {
            SerializedObject soVida = new SerializedObject(vida);
            SerializedProperty propBarra = soVida.FindProperty("barraVida");
            if (propBarra != null)
                propBarra.objectReferenceValue = null;

            SerializedProperty propTexto = soVida.FindProperty("textoVida");
            if (propTexto != null)
                propTexto.objectReferenceValue = null;

            soVida.ApplyModifiedProperties();
        }

        string caminho = $"{PastaSistema}/Prefab_Jogador.prefab";
        PrefabUtility.SaveAsPrefabAsset(clone, caminho);
        Object.DestroyImmediate(clone);
        return true;
    }

    private static bool GerarPrefabHUD()
    {
        Canvas canvasOrigem = Object.FindFirstObjectByType<Canvas>();
        InventarioUI invUiOrigem = Object.FindFirstObjectByType<InventarioUI>();
        GameObject origem = invUiOrigem != null
            ? invUiOrigem.gameObject
            : (canvasOrigem != null ? canvasOrigem.gameObject : null);

        if (origem == null)
        {
            Debug.LogWarning("[GeradorPrefabsSistema] Canvas HUD não encontrado na cena atual.");
            return false;
        }

        GameObject clone = Object.Instantiate(origem);
        clone.name = "Prefab_HUD_Sistema";

        // Garantir HUDVida conectado ao filho BarraVida interno
        HUDVida hudVida = clone.GetComponent<HUDVida>();
        if (hudVida == null)
            hudVida = clone.AddComponent<HUDVida>();

        Image barraImg = EncontrarComponenteEmFilhos<Image>(clone.transform, "BarraVida");
        TextMeshProUGUI textoVida = EncontrarComponenteEmFilhos<TextMeshProUGUI>(clone.transform, "Texto_Vida");

        // Incluir EventSystem interno com proteção contra duplicação
        EventSystem esFilho = clone.GetComponentInChildren<EventSystem>(true);
        if (esFilho == null)
        {
            EventSystem esCena = Object.FindFirstObjectByType<EventSystem>();
            GameObject esObj = esCena != null
                ? Object.Instantiate(esCena.gameObject, clone.transform)
                : new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            esObj.name = "EventSystem";
            esFilho = esObj.GetComponent<EventSystem>();
        }

        SerializedObject soHudVida = new SerializedObject(hudVida);
        if (barraImg != null)
            soHudVida.FindProperty("barraVida").objectReferenceValue = barraImg;
        if (textoVida != null)
            soHudVida.FindProperty("textoVida").objectReferenceValue = textoVida;
        if (esFilho != null)
            soHudVida.FindProperty("eventSystemLocal").objectReferenceValue = esFilho;
        soHudVida.ApplyModifiedProperties();

        // Limpar referências externas no InventarioUI e HUDInventario (auto-resolvidas em runtime)
        InventarioUI invUI = clone.GetComponent<InventarioUI>();
        if (invUI != null)
        {
            SerializedObject soInvUI = new SerializedObject(invUI);
            soInvUI.FindProperty("inventario").objectReferenceValue = null;
            SerializedProperty propBloq = soInvUI.FindProperty("componentesBloqueados");
            if (propBloq != null)
                propBloq.arraySize = 0;
            soInvUI.ApplyModifiedProperties();
        }

        HUDInventario hudInv = clone.GetComponent<HUDInventario>();
        if (hudInv != null)
        {
            SerializedObject soHudInv = new SerializedObject(hudInv);
            soHudInv.FindProperty("inventario").objectReferenceValue = null;
            soHudInv.FindProperty("objetivo").objectReferenceValue = null;
            soHudInv.ApplyModifiedProperties();
        }

        string caminho = $"{PastaSistema}/Prefab_HUD_Sistema.prefab";
        PrefabUtility.SaveAsPrefabAsset(clone, caminho);
        Object.DestroyImmediate(clone);
        return true;
    }

    private static bool GerarPrefabCamera()
    {
        GameObject origem = GameObject.Find("CAMERA");
        if (origem == null)
        {
            CinemachineCamera cmCam = Object.FindFirstObjectByType<CinemachineCamera>();
            if (cmCam != null)
                origem = cmCam.transform.parent != null ? cmCam.transform.parent.gameObject : cmCam.gameObject;
        }

        if (origem == null)
        {
            Debug.LogWarning("[GeradorPrefabsSistema] Objeto CAMERA não encontrado na cena atual.");
            return false;
        }

        GameObject clone = Object.Instantiate(origem);
        clone.name = "Prefab_Camera";
        clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        CinemachineCamera cm = clone.GetComponentInChildren<CinemachineCamera>(true);
        if (cm != null)
        {
            if (cm.GetComponent<VinculadorCameraJogador>() == null)
                cm.gameObject.AddComponent<VinculadorCameraJogador>();

            CameraTarget target = cm.Target;
            target.TrackingTarget = null;
            target.LookAtTarget = null;
            cm.Target = target;
        }

        string caminho = $"{PastaSistema}/Prefab_Camera.prefab";
        PrefabUtility.SaveAsPrefabAsset(clone, caminho);
        Object.DestroyImmediate(clone);
        return true;
    }

    private static T EncontrarComponenteEmFilhos<T>(Transform raiz, string nomeExato) where T : Component
    {
        foreach (T comp in raiz.GetComponentsInChildren<T>(true))
        {
            if (comp.gameObject.name == nomeExato)
                return comp;
        }
        return null;
    }

    private static void GarantirPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho))
            return;

        string[] partes = caminho.Split('/');
        string atual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string proximo = $"{atual}/{partes[i]}";
            if (!AssetDatabase.IsValidFolder(proximo))
            {
                AssetDatabase.CreateFolder(atual, partes[i]);
            }
            atual = proximo;
        }
    }
}
