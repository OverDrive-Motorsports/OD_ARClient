/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## EntityCollectionSync - Reconciles a list of entities in place against
 ## freshly fetched DTOs: updates entities whose key is still present, adds
 ## one for each new key, drops entities whose key disappeared. Existing
 ## instances are mutated (via TryApply), never replaced, so anything already
 ## holding a reference to one of them keeps pointing at valid, current data.
 ##
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public static class EntityCollectionSync
{
    // A dto that fails TryApply is skipped and logged; if that dto's key belonged to an
    // already-tracked entity, that entity gets removed too (this fetch no longer confirms it).
    public static void Sync<TDto, TEntity, TKey>(
        List<TEntity> target,
        List<TDto> dtos,
        Func<TDto, TKey> dtoKey,
        Func<TEntity, TKey> entityKey,
        Func<TEntity> createEntity,
        Func<TDto, TEntity, string> tryApply)
    {
        if (dtos == null) return;

        var seenKeys = new HashSet<TKey>();

        foreach (var dto in dtos)
        {
            TKey key = dtoKey(dto);
            TEntity entity = target.Find(e => Equals(entityKey(e), key));
            bool isNew = entity == null;
            if (isNew) entity = createEntity();

            string error = tryApply(dto, entity);
            if (error != null)
            {
                Debug.LogWarning($"[EntityCollectionSync] {error}");
                continue;
            }

            seenKeys.Add(key);
            if (isNew) target.Add(entity);
        }

        target.RemoveAll(e => !seenKeys.Contains(entityKey(e)));
    }
}
