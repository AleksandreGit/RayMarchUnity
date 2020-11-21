using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))] // We have to have a camera component
[ExecuteInEditMode, ImageEffectAllowedInSceneView] // execute in edit mode, so that the raymarch shader is always updated on camera
public class RaymarchCamera : MonoBehaviour
{
    /* SHADER */
    // We need to have a reference to our shader
    // That's why we get it through the instructor with [SerializeField]
    [SerializeField] 
    private Shader _shader;

    /* MATERIAL */
    private Material _rayMarchMat;

    public Material _rayMarchMaterial {
        get{
            if(!_rayMarchMat && _shader){
                _rayMarchMat = new Material(_shader);
                _rayMarchMat.hideFlags = HideFlags.HideAndDontSave;
            }
            return _rayMarchMat;
        }
    }

    /* CAMERA */
    private Camera _cam;
    public float _maxDistance;

    public Camera _camera {
        get {
            if(!_cam){
                _cam = GetComponent<Camera>();
            }
            return _cam;
        }
    }

    [Header("Setup")]
    [Range(0, 500)]
    public int _maxIterations;
    [Range(0.1f, 0.001f)]
    public float _accuracy;

    [Header("Directionnal light")]
    public Transform _directionnalLight;
    public Color _lightColor;
    public float _lightIntensity;

    [Header("Shading")]
    [Range(0, 4)]
    public float _shadowIntensity;
    public Vector2 _shadowDist;
    [Range(1, 128)]
    public float _shadowPenumbra;


    [Header("Signed Distance Field")]
    public Color _mainColor;
    public Vector4 _sphere;
    public float _sphereSmooth;
    public float _degreeRotate;
    /*
    public Vector4 _sphere1;
    public Vector4 _box1;
    public float _box1Round;
    public float _boxSphereSmooth;
    public Vector4 _sphere2;
    public float _sphereIntersectSmooth;*/

    [Header("Ambien occlusion")]
    [Range(0.01f, 10.0f)]
    public float _ambientOcclStepSize;
    [Range(0, 1)]
    public float _ambientOcclIntensity;
    [Range(1, 5)]
    public int _ambientOcclIterations;

    [Header("Reflection")]
    [Range(0, 2)]
    public int _reflectionCount;
    [Range(0, 1)]
    public float _reflectionIntensity;
    [Range(0, 1)]
    public float _envRefIntensity;
    public Cubemap _reflectionCube;

    /* RENDERING */
    /* link to the documentation : https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html */
    /* Appelé après chaque rendu.
     * Permet de modifier l'imga finale en lui appliquant des filtres de base.
     * L'image en entrée est src, l'image en sortie après process est dest.
     * On doit toujours soit utiliser Graphics.Blit(...) ou rendre un quad en 
     * fullscreen si on override cette méthode.
     * 
     */
    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if(!_rayMarchMaterial) {
            Graphics.Blit(src, dest);
            return;
        }

        // We assign the entrant elements to our shader
        _rayMarchMaterial.SetVector("_lightDirection", _directionnalLight ? _directionnalLight.forward : Vector3.down);
        _rayMarchMaterial.SetMatrix("_CamFrustum", CamFrustum(_camera));
        _rayMarchMaterial.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _rayMarchMaterial.SetFloat("_maxDistance", _maxDistance);
        _rayMarchMaterial.SetColor("_mainColor", _mainColor);
        _rayMarchMaterial.SetColor("_lightColor", _lightColor);
        _rayMarchMaterial.SetFloat("_lightIntensity", _lightIntensity);
        _rayMarchMaterial.SetFloat("_shadowIntensity", _shadowIntensity);
        _rayMarchMaterial.SetFloat("_shadowPenumbra", _shadowPenumbra);
        _rayMarchMaterial.SetFloat("_accuracy", _accuracy);
        _rayMarchMaterial.SetInt("_maxIterations", _maxIterations);
        _rayMarchMaterial.SetVector("_shadowDist", _shadowDist);
        _rayMarchMaterial.SetFloat("_ambientOcclStepSize", _ambientOcclStepSize);
        _rayMarchMaterial.SetFloat("_ambientOcclIntensity", _ambientOcclIntensity);
        _rayMarchMaterial.SetInt("_ambientOcclIterations", _ambientOcclIterations);
        _rayMarchMaterial.SetVector("_sphere", _sphere);
        _rayMarchMaterial.SetFloat("_sphereSmooth", _sphereSmooth);
        _rayMarchMaterial.SetFloat("_degreeRotate", _degreeRotate);

        // Reflection
        _rayMarchMaterial.SetFloat("_reflectionIntensity", _reflectionIntensity);
        _rayMarchMaterial.SetFloat("_envRefIntensity", _envRefIntensity);
        _rayMarchMaterial.SetInt("_reflectionCount", _reflectionCount);
        _rayMarchMaterial.SetTexture("_reflectionCube", _reflectionCube); 

    RenderTexture.active = dest; // We now draw the quad on which we will draw the output of the shader
        _rayMarchMaterial.SetTexture("_MainTex", src);
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

        //TR
        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f);

        //TL
        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        GL.End();
        GL.PopMatrix();
    }

    // Calcul de la matrice représentant le frustum
    private Matrix4x4 CamFrustum(Camera cam) {
        Matrix4x4 frustum = Matrix4x4.identity; // Matrice identité
        // Calcul du champ de vision
        float fov = Mathf.Tan((cam.fieldOfView*0.5f) * Mathf.Deg2Rad);

        // Caclul des vecteurs direction de la caméra
        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;

        // Définition des points du quad
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
