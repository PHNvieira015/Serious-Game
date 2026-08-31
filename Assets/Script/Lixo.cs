using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
public class Lixo : MonoBehaviour
{
    Camera cam;
    Collider col;
    LayerMask camadaLixeira;


    private void Start()
    {
        cam = Camera.main;
        col = cam.GetComponent<Collider>();
    }

    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown");

    }


    private void OnMouseDrag()
    {
        Debug.Log("OnMouseDrag");
        Ray raio = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(raio, out RaycastHit hit))
        {
            Vector3 novaPosicao = hit.point;
            novaPosicao.y += 1;
            transform.position = hit.point;
        }
    }

    private void OnMouseUp()
    {

        Debug.Log("OnMOuseUp");
        {
            Ray raio = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(raio, out RaycastHit hit, 100f, camadaLixeira))
            {
                {
                Acertou();

                }
            }
            Errou();

        }
    }

    void Acertou()
    {
        Debug.Log("Acertou uma Lixeira");
    }

    void Errou()
    {
        Debug.Log("Errou uma Lixeira");

    }
}
