using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))] // We have to have a camera component
[ExecuteInEditMode] // execute in edit mode, so that the raymarch shader is always updated on camera
public class RaymarchCamera : MonoBehaviour
{
    /* SHADER */
    // We need to have a reference to our shader
    [SerializeField] // to show it into the inspector
    private Shader _shader;

    // We have to create a material linked to the shader
    public Material _rayMarchMaterial {
        get{
            if(_rayMarchMat && _shader){
                _rayMarchMat = new Material(_shader);
                _rayMarchMat.hideFlags = HideFlags.HideAndDontSave;
            }
            return _rayMarchMat;
        }
    }

    private Material _rayMarchMat;

    /* CAMERA */
    public Camera _camera {
        get {
            if(!_cam){
                _cam = GetComponent<Camera>();
            }
            return _cam;
        }
    }
    private Camera _cam;

    /* RENDERING */
    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if(!_rayMarchMaterial) {
            Graphics.Blit(src, dest);
            return;
        }

        // We assign the entrant elements to our shader
        _rayMarchMaterial.SetMatrix("_CamFrustum", CamFrustum(_camera));
        _rayMarchMaterial.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _rayMarchMaterial.SetVector("_CamWorldSpace", _camera.transform.position);

        RenderTexture.active = dest; // We now draw the quad on which we will draw the output of the shader
        GL.PushMatrix();
        GL.LoadOrtho();
        _rayMarchMaterial.SetPass(0);
        GL.Begin(GL.QUADS);

        //BL
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f);

        //BR
        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f);

        //TP
        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 0.0f, 1.0f);

        //TL
        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        GL.End();
        GL.PopMatrix();
    }

    private Matrix4x4 CamFrustum(Camera cam) {
        Matrix4x4 frustum = Matrix4x4.identity; // Matrice identité
        float fov = Mathf.Tan((cam.fieldOfView*0.5f) * Mathf.Deg2Rad);

        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;

        Vector3 TL = (-Vector3.forward - goRight + goUp); // top-left corner
        Vector3 TR = (-Vector3.forward + goRight + goUp); // top-right corner
        Vector3 BR = (-Vector3.forward + goRight - goUp); // top-left corner
        Vector3 BL = (-Vector3.forward - goRight - goUp); // top-left corner

        frustum.SetRow(0, TL);
        frustum.SetRow(1, TR);
        frustum.SetRow(2, BR);
        frustum.SetRow(3, BL);

        return frustum;
    }
}
