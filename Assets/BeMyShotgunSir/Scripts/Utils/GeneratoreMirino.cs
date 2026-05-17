using UnityEngine;
using UnityEditor;
using System.IO;

public class GeneratoreMirino
{
    [MenuItem("Tools/Genera Mirino PNG")]
    public static void Genera()
    {
        int size = 512; // Risoluzione alta
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color trasparente = new Color(0, 0, 0, 0);
        Color bianco = Color.white;
        Color red = new Color(1f, 0.1f, 0.1f, 1f);

        Vector2 centro = new Vector2(size / 2f, size / 2f);
        float raggio = 200f;
        float spessore = 20f;

        // Puliamo la texture (tutto trasparente)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, trasparente);
            }
        }

        // Disegniamo l'anello
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distanza = Vector2.Distance(new Vector2(x, y), centro);

                // Se il pixel è dentro il bordo dell'anello
                if (distanza > raggio - spessore && distanza < raggio + spessore)
                {
                    // Creiamo 4 "tagli" per dare l'effetto mirino arcade
                    bool taglioOrizzontale = (y > size / 2 - 30 && y < size / 2 + 30);
                    bool taglioVerticale = (x > size / 2 - 30 && x < size / 2 + 30);

                    if (!taglioOrizzontale && !taglioVerticale)
                    {
                        tex.SetPixel(x, y, red);
                    }
                }
            }
        }

        tex.Apply();

        // Salviamo il file PNG
        byte[] bytes = tex.EncodeToPNG();
        string percorso = Application.dataPath + "/Mirino.png";
        File.WriteAllBytes(percorso, bytes);

        // Aggiorniamo l'editor per far apparire il file
        AssetDatabase.Refresh();

        Debug.Log("Fatto! Il file Mirino.png è stato creato nella tua cartella Assets principale.");
    }
}