using UnityEngine;

/// <summary>
/// Runtime-built 2D particle emitters for footsteps, dig debris, and gem sparkles.
/// </summary>
public static class GameplayFxParticles
{
    static Material dustMaterial;
    static Material pebbleMaterial;
    static Material sparkleMaterial;

    static Material CreateSpriteMaterial(Texture2D texture)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        var mat = shader != null ? new Material(shader) : null;
        if (mat != null && texture != null)
            mat.mainTexture = texture;
        return mat;
    }

    static Material DustMaterial()
    {
        if (dustMaterial == null)
            dustMaterial = CreateSpriteMaterial(GameplaySprites.DustSpeckTexture);
        return dustMaterial;
    }

    static Material PebbleMaterial()
    {
        if (pebbleMaterial == null)
            pebbleMaterial = CreateSpriteMaterial(GameplaySprites.PebbleTexture);
        return pebbleMaterial;
    }

    static Material SparkleMaterial()
    {
        if (sparkleMaterial == null)
            sparkleMaterial = CreateSpriteMaterial(GameplaySprites.StarSparkleTexture);
        return sparkleMaterial;
    }

    public static ParticleSystem CreateFootDust(Transform parent)
    {
        var go = new GameObject("FootDust");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.66f, 0.62f, 0.55f, 0.80f),
            new Color(0.72f, 0.68f, 0.60f, 0.85f));
        main.gravityModifier = -0.05f;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.03f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(0.10f, 0.25f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOver.color = grad;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.40f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.material = DustMaterial();
        return ps;
    }

    public static ParticleSystem CreateDigDebris(Transform parent)
    {
        var go = new GameObject("DigDebris");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.36f, 0.22f, 0.12f, 1f),
            new Color(0.48f, 0.32f, 0.18f, 1f));
        main.gravityModifier = 1.6f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.06f;

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOver.color = grad;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.30f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 11;
        renderer.material = PebbleMaterial();
        return ps;
    }

    public static ParticleSystem CreateGemSparkle(Transform parent)
    {
        var go = new GameObject("GemSparkle");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.40f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.75f, 1f),
            new Color(1f, 0.80f, 0.35f, 1f));
        main.gravityModifier = 0.02f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.06f;

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.40f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOver.color = grad;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.25f, 1.2f),
            new Keyframe(1f, 0.2f));
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 15;
        renderer.material = SparkleMaterial();
        return ps;
    }

    public static void EmitBurst(ParticleSystem ps, Vector3 worldPosition, int count)
    {
        if (ps == null)
            return;
        ps.transform.position = worldPosition;
        ps.Emit(count);
    }
}
