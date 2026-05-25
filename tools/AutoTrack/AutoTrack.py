import bpy
import json
import math
from pathlib import Path

# === chemin du fichier ===
path = r"C:\Users\antho\Desktop\mc-1929.geojson"

# === chargement ===
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

feature = data["features"][0]
coords = feature["geometry"]["coordinates"]

# === origine locale : premier point ===
origin_lon, origin_lat = coords[0][:2]

# Conversion approximative degrés -> mètres
# 1° latitude ~= 111320 m
# 1° longitude ~= 111320 * cos(latitude)
lat_scale = 111320.0
lon_scale = 111320.0 * math.cos(math.radians(origin_lat))

points = []
for coord in coords:
    lon, lat = coord[:2]
    z = coord[2] if len(coord) > 2 else 0.0

    x = (lon - origin_lon) * lon_scale
    y = (lat - origin_lat) * lat_scale

    points.append((x, y, z))

# === créer une collection dédiée ===
collection_name = "Circuits"
if collection_name in bpy.data.collections:
    collection = bpy.data.collections[collection_name]
else:
    collection = bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(collection)

# === supprimer ancienne courbe si elle existe ===
obj_name = feature["properties"].get("Name", "GeoJSON_Track")
if obj_name in bpy.data.objects:
    old_obj = bpy.data.objects[obj_name]
    bpy.data.objects.remove(old_obj, do_unlink=True)

# === créer la courbe ===
curve_data = bpy.data.curves.new(name=obj_name + "_Curve", type='CURVE')
curve_data.dimensions = '3D'
curve_data.resolution_u = 2

spline = curve_data.splines.new('POLY')
spline.points.add(len(points) - 1)

for i, (x, y, z) in enumerate(points):
    spline.points[i].co = (x, y, z, 1.0)

# ferme la boucle si le circuit doit être fermé
spline.use_cyclic_u = True

curve_obj = bpy.data.objects.new(obj_name, curve_data)
collection.objects.link(curve_obj)

# --- profil plat pour faire un ruban ---
profile_name = "TrackProfile"

if profile_name in bpy.data.objects:
    profile_obj = bpy.data.objects[profile_name]
else:
    profile_curve = bpy.data.curves.new(profile_name, type='CURVE')
    profile_curve.dimensions = '2D'

    profile_spline = profile_curve.splines.new('POLY')
    profile_spline.points.add(1)

    half_width = 10.0  # 4 m de large visuellement
    profile_spline.points[0].co = (-half_width, 0, 0, 1)
    profile_spline.points[1].co = ( half_width, 0, 0, 1)

    profile_obj = bpy.data.objects.new(profile_name, profile_curve)
    bpy.context.scene.collection.objects.link(profile_obj)

# appliquer le profil à la courbe du circuit
curve_data.bevel_depth = 0.0
curve_data.bevel_object = profile_obj
curve_data.fill_mode = 'FULL'

print(f"Courbe créée : {obj_name} avec {len(points)} points")