using System;
using UnityEngine;
using DG.Tweening;

public static class Utils
{
    public static void PlayButtonClickTween(RectTransform target, Action onComplete, float pressedScale = 0.92f)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        target.DOKill();
        target.localScale = Vector3.one;

        Sequence clickSequence = DOTween.Sequence();
        clickSequence.Append(target.DOScale(pressedScale, 0.07f).SetEase(Ease.OutQuad));
        clickSequence.Append(target.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
        clickSequence.OnComplete(() => onComplete?.Invoke());
    }

    public static RectTransform ResolveUIAnimationTransform(Transform root, string preferredChildName = "AnimRoot")
    {
        if (root == null)
            return null;

        if (!string.IsNullOrEmpty(preferredChildName))
        {
            Transform namedChild = root.Find(preferredChildName);
            if (namedChild is RectTransform namedRect)
                return namedRect;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i) is RectTransform childRect)
                return childRect;
        }

        return null;
    }

    public static string FormatNumber(int value)
    {
        if (value < 1000) return value.ToString(); // less than 1000, just return the number as is

        // suffixes array, can be extended if needed
        string[] suffixes = { "", "k", "m", "b", "t", "q", "Q", "s", "S" };
        int i = 0;
        double dValue = (double)value;

        // divide until less than 1000
        while (dValue >= 1000 && i < suffixes.Length - 1)
        {
            dValue /= 1000;
            i++;
        }

        double roundedDown = Math.Floor(dValue * 10) / 10; // round down to 1 decimal place

        // format to 1 decimal place and add suffix
        return roundedDown.ToString("0.#") + suffixes[i];
    }
}