"""Original asset finish pass: UV textures, deform armatures, bound meshes, real Cycles renders.
Blender 4.3: blender -b --python build_portfolio.py -- original.blend output_folder
Textures are authored mathematically here; no downloaded art or image-generation is used.
"""
import bpy, math, json, os, sys
import numpy as np
from mathutils import Vector, Matrix
source, out = sys.argv[sys.argv.index('--')+1:][:2]
os.makedirs(out,exist_ok=True)
for sub in ('Models','Textures','Renders'): os.makedirs(os.path.join(out,sub),exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=source)
C=Matrix(((1,0,0),(0,0,-1),(0,1,0)))
Ci=C.inverted()
N=512
Y,X=np.mgrid[0:N,0:N].astype(np.float32)/N
rng=np.random.default_rng(20191018)
noise=rng.random((N,N),dtype=np.float32)
# Tileable analytic patterns. Colour maps remain near white; material tint supplies the palette.
patterns={
 'Cloth':(.84+.10*np.sin(X*math.tau*96)*np.sin(Y*math.tau*96)+.04*noise),
 'Wood':(.78+.12*np.sin(math.tau*(X*18+.32*np.sin(Y*math.tau*2)))+.06*np.sin(X*math.tau*71)+.04*noise),
 'Metal':(.88+.04*np.sin(X*math.tau*113)+.035*noise),
 'Leather':(.8+.11*noise+.055*np.sin(X*math.tau*32)*np.cos(Y*math.tau*29)),
 'Bone':(.88+.045*np.sin(X*math.tau*4)*np.sin(Y*math.tau*3)+.035*noise),
 'Stone':(.82+.10*noise+.04*np.cos(X*math.tau*5)*np.cos(Y*math.tau*7))}
family={'Navy':'Cloth','Blue':'Cloth','Red':'Cloth','Wood':'Wood','WoodLight':'Wood','Gold':'Metal','Steel':'Metal','Leather':'Leather','Bone':'Bone','Stone':'Stone'}
texture_images={}
def saveimage(name,values,srgb=True):
    im=bpy.data.images.new(name,width=N,height=N,alpha=True)
    im.colorspace_settings.name='sRGB' if srgb else 'Non-Color'
    rgba=np.ones((N,N,4),dtype=np.float32)
    if values.ndim==2: rgba[:,:,:3]=values[:,:,None]
    else: rgba[:,:,:3]=values
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=os.path.join(out,'Textures',name+'.png');im.file_format='PNG';im.save()
    texture_images[name]=im;return im
for name,pat in patterns.items():
    saveimage(name+'_Color',np.clip(pat,0,1))
    dx=(np.roll(pat,-1,axis=1)-np.roll(pat,1,axis=1))*1.1
    dy=(np.roll(pat,-1,axis=0)-np.roll(pat,1,axis=0))*1.1
    normal=np.stack([-dx,-dy,np.ones_like(dx)],axis=-1);normal/=np.linalg.norm(normal,axis=2,keepdims=True)
    saveimage(name+'_Normal',normal*.5+.5,False)
# Brighten the dark nautical palette, while preserving the original design.
colors={'Navy':'264B70','Blue':'388EAC','Gold':'DDAE58','Steel':'B8D5DE','Leather':'514048','Skin':'EDBD96','Hair':'DFE9E6','Bone':'E9DCC0','Dark':'152C37','Wood':'72472C','WoodLight':'B47B46','Teal':'50DCD4','Red':'AD5B48','Stone':'45616C'}
def rgb(s): return tuple(int(s[i:i+2],16)/255 for i in (0,2,4))
def lin(c): return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c)
materials=[]
for name,col in colors.items():
    mat=bpy.data.materials[name];bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*lin(rgb(col)),1)
    mat.diffuse_color=(*rgb(col),1)
    rough=float(bs.inputs['Roughness'].default_value);metal=float(bs.inputs['Metallic'].default_value)
    fam=family.get(name,'')
    if fam:
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=texture_images[fam+'_Color'];tex.extension='REPEAT'
        mul=mat.node_tree.nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=(*lin(rgb(col)),1)
        mat.node_tree.links.new(tex.outputs['Color'],mul.inputs[1]);mat.node_tree.links.new(mul.outputs[0],bs.inputs['Base Color'])
        norm=mat.node_tree.nodes.new('ShaderNodeTexImage');norm.image=texture_images[fam+'_Normal']
        node=mat.node_tree.nodes.new('ShaderNodeNormalMap');node.inputs['Strength'].default_value=.32
        mat.node_tree.links.new(norm.outputs['Color'],node.inputs['Color']);mat.node_tree.links.new(node.outputs[0],bs.inputs['Normal'])
    materials.append(dict(name=name,color=[*rgb(col),1],metallic=metal,roughness=rough,texture=fam))
roots=[bpy.data.objects[n] for n in ['Hero','Skeleton','Chest','Sword1','Sword2','Sword3']]
def desc(root):
    result=[root]
    for child in root.children: result+=desc(child)
    return result
def ancestors(o):
    a=[]
    while o.parent:
        o=o.parent;a.append(o)
    return a
kept=set(o for root in roots for o in desc(root))
for o in list(bpy.data.objects):
    if o not in kept: bpy.data.objects.remove(o,do_unlink=True)
for root in roots:
    root.location=(0,0,0);root.rotation_euler=(0,0,0)
bpy.context.view_layer.update()
# Seams/buttons are modeled details, not labels painted onto the final picture.
def stitch(name,a,b,root):
    av=C@Vector(a);bv=C@Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=.004,depth=(bv-av).length)
    o=bpy.context.object;o.name=name;o.parent=root;o.location=(av+bv)/2;o.rotation_euler=(bv-av).to_track_quat('Z','Y').to_euler();o.data.materials.append(bpy.data.materials['Gold']);return o
for side in (-1,1):
    for y in np.arange(1.03,1.5,.045): stitch('CoatStitch',(side*.22,float(y),.267),(side*.22,float(y+.018),.267),roots[0])
# Bones are real Armature data; mesh vertex groups + armature modifiers are created below.
def bones_for(name):
    if name=='Chest': return [('Root',-1,(0,0,0),(0,.3,0)),('Lid',0,(0,.61,-.36),(0,.61,.36))]
    if name.startswith('Sword'):return [('Root',-1,(0,0,0),(0,1.2,0))]
    hero=name=='Hero';hip=.91 if hero else .87;shoulder=1.49 if hero else 1.4;sx=.33 if hero else .26
    b=[('Root',-1,(0,0,0),(0,.35,0)),('Hips',0,(0,hip,0),(0,1.12,0)),('Spine',1,(0,1.12,0),(0,1.53,0)),('Head',2,(0,1.58,0),(0,2.05,0))]
    for side,label in [(-1,'Left'),(1,'Right')]:
        i=len(b);b += [(label+'Arm',2,(side*sx,shoulder,0),(side*(sx+.06),shoulder-.28,.015)),(label+'Forearm',i,(side*(sx+.06),shoulder-.28,.015),(side*(sx+.10),shoulder-.56,.05)),(label+'Hand',i+1,(side*(sx+.10),shoulder-.56,.05),(side*(sx+.10),shoulder-.72,.06))]
        i=len(b);b += [(label+'Leg',1,(side*.14,hip,0),(side*.15,hip-.39,0)),(label+'Shin',i,(side*.15,hip-.39,0),(side*.14,.1,.04)),(label+'Foot',i+1,(side*.14,.1,.04),(side*.14,.1,.25))]
    return b
rigs={};exports={}
for root in roots:
    name=root.name;spec=bones_for(name);indices={b[0]:i for i,b in enumerate(spec)}
    ar=bpy.data.armatures.new(name+'_DeformSkeleton');rig=bpy.data.objects.new(name+'_Rig',ar);bpy.context.collection.objects.link(rig);rig.show_in_front=True
    bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    for bn,pa,head,tail in spec:
        eb=ar.edit_bones.new(bn);eb.head=C@Vector(head);eb.tail=C@Vector(tail)
        if pa>=0: eb.parent=ar.edit_bones[spec[pa][0]]
    bpy.ops.object.mode_set(mode='OBJECT');rigs[name]=rig
    data=dict(name=name,materials=materials,bones=[dict(name=bn,parent=pa,head=list(head),tail=list(tail)) for bn,pa,head,tail in spec],vertices=[],uv=[],triangles=[],triangleMaterials=[],bone0=[],bone1=[],weight0=[])
    def weight(o,p):
        if name=='Chest':return (1,1,1.) if any(a.name.startswith('LidPivot') for a in [o]+ancestors(o)) else (0,0,1.)
        if name.startswith('Sword'):return (0,0,1.)
        chain=[a.name.split('.')[0] for a in [o]+ancestors(o)]
        for label in ('Left','Right'):
            if label+'Arm' in chain:
                if any(a in chain for a in ['WeaponMount','HeldSword','SkeletonSword']):return indices[label+'Hand'],indices[label+'Hand'],1.
                if p.y<(1.49 if name=='Hero' else 1.4)-.54:return indices[label+'Hand'],indices[label+'Hand'],1.
                blend=max(0.,min(1.,(p.y-((1.49 if name=='Hero' else 1.4)-.34))/.15))
                return indices[label+'Arm'],indices[label+'Forearm'],blend
            if label+'Leg' in chain:
                if p.y<.2:return indices[label+'Foot'],indices[label+'Foot'],1.
                blend=max(0.,min(1.,(p.y-.43)/.2));return indices[label+'Leg'],indices[label+'Shin'],blend
        if p.y>1.6:return 3,3,1.
        return (1,1,1.) if p.y<1.02 else (2,2,1.)
    for o in desc(root):
        if o.type!='MESH':continue
        me=o.data;me.calc_loop_triangles();uvlayer=me.uv_layers.active or me.uv_layers.new(name='CraftUV')
        world=o.matrix_world.copy()
        # Per-face box UV projection; seams are deliberate on the faceted style.
        for face in me.polygons:
            n=world.to_3x3()@face.normal;axis=max(range(3),key=lambda a:abs(n[a]));axes=[a for a in range(3) if a!=axis]
            for li in face.loop_indices:
                p=world@me.vertices[me.loops[li].vertex_index].co
                uvlayer.data[li].uv=(p[axes[0]]*.85,p[axes[1]]*.85)
        groups=[o.vertex_groups.new(name=b[0]) for b in spec]
        for v in me.vertices:
            p=Ci@(world@v.co);i,j,w=weight(o,p)
            if i==j:groups[i].add([v.index],1.,'REPLACE')
            else:
                if w>0:groups[i].add([v.index],w,'REPLACE')
                if w<1:groups[j].add([v.index],1-w,'REPLACE')
        mod=o.modifiers.new('WeightedArmature','ARMATURE');mod.object=rig
        mid=list(colors).index(o.data.materials[0].name)
        for tri in me.loop_triangles:
            start=len(data['vertices'])//3
            for li in tri.loops:
                p=Ci@(world@me.vertices[me.loops[li].vertex_index].co);i,j,w=weight(o,p)
                data['vertices']+=list(p);data['uv']+=list(uvlayer.data[li].uv)
                data['bone0'].append(i);data['bone1'].append(j);data['weight0'].append(w)
            data['triangles'] += [start,start+1,start+2];data['triangleMaterials'].append(mid)
    with open(os.path.join(out,'Models',name+'.json'),'w') as f:json.dump(data,f,separators=(',',':'))
    exports[name]=data
    # Group model and matching armature together after binding.
    holder=bpy.data.objects.new(name+'_Display',None);bpy.context.collection.objects.link(holder);root.parent=holder;rig.parent=holder
    rig.select_set(False)
# A subtle breathing / inspection action with actual keyed deform bones in Blender.
for name in ('Hero','Skeleton'):
    rig=rigs[name]
    for frame,angle in [(1,0),(24,.17),(48,0),(72,-.17),(96,0)]:
        for bn,m in [('LeftArm',1),('RightArm',-1),('LeftLeg',-1),('RightLeg',1)]:
            pb=rig.pose.bones[bn];pb.rotation_mode='XYZ';pb.rotation_euler[0]=angle*m;pb.keyframe_insert(data_path='rotation_euler',frame=frame)
    rig.animation_data.action.name=name+'_InspectionWalk_96f'
    rig.animation_data.action.use_fake_user=True
# Chest lid opening, using the actual hinge bone local X axis.
rig=rigs['Chest'];pb=rig.pose.bones['Lid'];pb.rotation_mode='XYZ'
for frame,angle in [(1,0),(32,0),(64,math.radians(-100)),(96,0)]:
    pb.rotation_euler[0]=angle;pb.keyframe_insert(data_path='rotation_euler',frame=frame)
rig.animation_data.action.name='Chest_Hinge_Open';rig.animation_data.action.use_fake_user=True
scene=bpy.context.scene;scene.frame_set(1);scene.frame_end=96
# Studio cyclorama and low individual plinths.
def mat(name,color,metal=0,rough=.5):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*lin(color),1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough;return m
floor=mat('StudioFloor',(.11,.18,.23),.2,.38);plinth=mat('Plinth',(.22,.32,.37),.35,.32)
def cube(name,pos,scale,material,bev=.04):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    if bev:
        m=o.modifiers.new('Rounded edges','BEVEL');m.width=bev;m.segments=3
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o
cube('Studio ground',(0,0,-.24),(200,200,.2),floor,.0)
layout={'Hero':(-1.9,0,0),'Skeleton':(0,0,0),'Chest':(2,0,.25),'Sword1':(1.45,1.15,.08),'Sword2':(2,1.15,.08),'Sword3':(2.55,1.15,.08)}
# layout tuples use Unity x,z,y here for readable asset placement.
for name,(x,z,y) in layout.items():bpy.data.objects[name+'_Display'].location=(x,-z,y)
for name,x,width in [('Hero',-1.9,1.35),('Skeleton',0,1.35),('Chest',2,1.65)]:cube(name+' plinth',(x,0,-.07 if name!='Chest' else .055),(width,1.25,.14 if name!='Chest' else .39),plinth)
for i,x in enumerate([1.45,2,2.55]):cube('Sword base'+str(i),(x,-1.15,-.03),(.4,.45,.16),plinth)
world=bpy.data.worlds.new('Studio environment');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.28,.34,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5
for name,loc,power,size,col in [('Key softbox',(-4,-5,7),1700,5,(.86,.94,1)),('Warm fill',(5,-3,4),1150,4,(1,.85,.65)),('Rim strip',(1,3,5),1900,3,(.5,.91,1))]:
    bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.name=name;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.data.color=col;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.lens=55
scene.render.engine='CYCLES';scene.cycles.samples=96;scene.cycles.use_denoising=False;scene.render.resolution_x=2200;scene.render.resolution_y=1600;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.render.image_settings.file_format='PNG'
def camera(pos,target,scale):cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
camera((6,-10,5),(0,0,.95),7.6)
# Pack texture files for a portable source document.
for image in texture_images.values():image.pack()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Tideward_Portfolio.blend'))
shots=[('01_Asset_Collection',(6,-10,5),(0,-.2,1),7.6),('02_Hero_Materials',(-.1,-5,2.8),(-1.9,0,1.13),3.9),('03_Skeleton_Details',(2,-5,2.6),(0,0,1.05),3.65),('04_Weapons_Chest',(4,-5,3),(2,-.6,.72),3.6)]
for si,(filename,pos,target,scale) in enumerate(shots):
    visible=[list(layout),['Hero'],['Skeleton'],['Chest','Sword1','Sword2','Sword3']][si]
    for name in layout:
        for obj in desc(bpy.data.objects[name+'_Display']):obj.hide_render=name not in visible
    for obj in bpy.data.objects:
        if obj.name.endswith(' plinth'):obj.hide_render=obj.name.split(' ')[0] not in visible
        if obj.name.startswith('Sword base'):obj.hide_render='Sword1' not in visible
    camera(pos,target,scale);scene.render.filepath=os.path.join(out,'Renders',filename+'.png');bpy.ops.render.render(write_still=True)
# Technical rig view: real armature heads/tails drawn as geometry for a renderable overlay.
# Offset the overlay toward the camera so the deform rig remains readable through the model.
for name in layout:
    for obj in desc(bpy.data.objects[name+'_Display']):obj.hide_render=name not in ('Hero','Skeleton')
for obj in bpy.data.objects:
    if obj.name.endswith(' plinth'):obj.hide_render=obj.name.split(' ')[0] not in ('Hero','Skeleton')
    if obj.name.startswith('Sword base'):obj.hide_render=True
rigmat=mat('Rig Cyan',(.08,.92,.88),.15,.3);bs=rigmat.node_tree.nodes['Principled BSDF'];bs.inputs['Emission Color'].default_value=(.06,.6,.52,1);bs.inputs['Emission Strength'].default_value=.3
for name in ('Hero','Skeleton'):
    rig=rigs[name]
    for b in rig.data.bones:
        a=rig.matrix_world@b.head_local+Vector((0,-.5,0));end=rig.matrix_world@b.tail_local+Vector((0,-.5,0));v=end-a
        bpy.ops.mesh.primitive_cone_add(vertices=4,radius1=.045,radius2=.012,depth=v.length,location=(a+end)/2);o=bpy.context.object;o.name='RigDisplay_'+name+'_'+b.name;o.rotation_euler=v.to_track_quat('Z','Y').to_euler();o.data.materials.append(rigmat)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.035,location=a);bpy.context.object.data.materials.append(rigmat)
camera((-.95,-9,2.6),(-.95,0,1.05),4.4);scene.render.filepath=os.path.join(out,'Renders','05_Deform_Rig.png');bpy.ops.render.render(write_still=True)
# Validate that deform modifiers actually move mesh vertices, not just armature overlays.
scene.frame_set(1);bpy.context.view_layer.update();reference={}
for name in ('Hero','Skeleton'):
    for o in desc(bpy.data.objects[name]):
        if o.type=='MESH':reference[o.name]=[v.co.copy() for v in o.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]
scene.frame_set(24);bpy.context.view_layer.update();moved=0
for objname,verts in reference.items():
    o=bpy.data.objects[objname].evaluated_get(bpy.context.evaluated_depsgraph_get())
    moved+=sum((a-b.co).length>1e-5 for a,b in zip(verts,o.data.vertices))
assert moved>100, 'Armature must deform actual vertices'
scene.frame_set(1)
report={'deformed_vertices_at_frame24':moved,'assets':{n:{'triangles':len(d['triangles'])//3,'bones':len(d['bones']),'weighted_vertices':len(d['bone0'])} for n,d in exports.items()},'textures':'Six authored tileable color/normal pairs, 512x512','render_engine':'Blender Cycles; not a Unity screenshot'}
with open(os.path.join(out,'validation.json'),'w') as f:json.dump(report,f,indent=2)
print('PORTFOLIO COMPLETE',json.dumps(report))
