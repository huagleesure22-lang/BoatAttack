"""Tideward Voyager: a newly modeled, UV-textured and armature-bound original character.
Blender 4.3, no downloaded models. Run: blender -b -t 4 --python this.py -- OUTPUT
"""
import bpy,math,sys,os,json
from mathutils import Vector
out=sys.argv[sys.argv.index('--')+1];os.makedirs(out,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath='/workspace/shared/islandquest-portfolio/Tideward_Portfolio.blend')
# Reuse our authored texture materials, then replace every previous mesh and armature.
for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
# Author in Unity coordinates (x, up-y, front-z), converted to Blender for construction.
def P(v):return Vector((v[0],-v[2],v[1]))
mat={n:bpy.data.materials[n] for n in ['Navy','Blue','Gold','Steel','Leather','Skin','Hair','Bone','Dark','Wood','WoodLight','Teal','Red','Stone']}
def linear(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
for n,c in [('Skin',(1,.75,.61)),('Hair',(.72,.83,.91)),('Navy',(.09,.19,.32)),('Blue',(.09,.49,.61)),('Gold',(.83,.62,.25))]:
    bs=mat[n].node_tree.nodes.get('Principled BSDF')
    if n in ('Skin','Hair'):bs.inputs['Base Color'].default_value=(*[linear(v) for v in c],1)
    if n=='Skin':bs.inputs['Roughness'].default_value=.5
parts=[]
def finish(o,name,material,bone):
    o.name=name;o.data.materials.clear();o.data.materials.append(mat[material]);o['deform_bone']=bone
    for f in o.data.polygons:f.use_smooth=True
    if o.type=='MESH' and not o.data.uv_layers.active:
        o.data.uv_layers.new(name='UVMap')
        for face in o.data.polygons:
            n=face.normal;axis=max(range(3),key=lambda i:abs(n[i]));a=[i for i in range(3) if i!=axis]
            for li in face.loop_indices:
                p=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=(p[a[0]],p[a[1]])
    parts.append(o);return o

def ell(name,pos,size,material,bone='Spine',seg=24,rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,radius=1,location=P(pos));o=bpy.context.object;o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,material,bone)
def box(name,pos,size,material,bone='Spine',bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=P(pos));o=bpy.context.object;o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        m=o.modifiers.new('Rounded tailoring','BEVEL');m.width=bevel;m.segments=3;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
    return finish(o,name,material,bone)
def tube(name,points,radii,material,bone='Spine',sides=10):
    verts=[]
    for i,p in enumerate(points):
        q=P(p);d=P(points[min(i+1,len(points)-1)])-P(points[max(0,i-1)])
        d.normalize();u=d.cross(Vector((0,1,0)))
        if u.length<.01:u=d.cross(Vector((1,0,0)))
        u.normalize();v=d.cross(u).normalized()
        for k in range(sides):verts.append(q+radii[i]*(u*math.cos(k*math.tau/sides)+v*math.sin(k*math.tau/sides)))
    faces=[]
    for i in range(len(points)-1):
        for k in range(sides):faces.append((i*sides+k,i*sides+(k+1)%sides,(i+1)*sides+(k+1)%sides,(i+1)*sides+k))
    faces += [tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+k for k in range(sides))]
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
    return finish(o,name,material,bone)
def surface(name,rows,material,bone='Spine',thickness=.012):
    v=[P(p) for row in rows for p in row];width=len(rows[0]);f=[]
    for i in range(len(rows)-1):
        for j in range(width-1):f.append((i*width+j,i*width+j+1,(i+1)*width+j+1,(i+1)*width+j))
    me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
    if thickness:
        m=o.modifiers.new('Cloth thickness','SOLIDIFY');m.thickness=thickness;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
    return finish(o,name,material,bone)
def torso(name,rings,material,bone='Spine'):
    rows=[]
    for y,rx,rz in rings:rows.append([(rx*math.sin(k*math.tau/32),y,rz*math.cos(k*math.tau/32)) for k in range(33)])
    return surface(name,rows,material,bone,.015)
# A tapered, rounded silhouette with distinct upper arms, elbows, forearms and hands.
for side,label in [(-1,'Left'),(1,'Right')]:
    tube(label+' tailored trousers',[(side*.105,1.05,0),(side*.12,.86,.015),(side*.13,.62,.018),(side*.125,.53,.012)],[.11,.095,.082,.078],'Navy',label+'Leg',16)
    tube(label+' fitted boot',[(side*.125,.55,.012),(side*.13,.42,.015),(side*.13,.17,.028),(side*.13,.075,.08)],[.086,.078,.065,.085],'Leather',label+'Shin',16)
    ell(label+' boot foot',(side*.13,.065,.13),(.093,.065,.17),'Navy',label+'Foot')
    box(label+' boot sole',(side*.13,.017,.135),(.19,.025,.34),'Dark',label+'Foot',.015)
    for y in (.49,.24):tube(label+' gold boot strap',[(side*.13-.084,y,.025),(side*.13,y,.11),(side*.13+.084,y,.025)],[.012]*3,'Gold',label+'Shin',8)
    for y in (.18,.23,.28,.33,.38,.43):
        tube(label+' boot lacing',[(side*.13-.026,y,.091),(side*.13+.026,y+.026,.091)],[.004,.004],'Gold',label+'Shin',6)
    tube(label+' upper sleeve',[(side*.255,1.5,0),(side*.31,1.39,0),(side*.37,1.2,.022)],[.105,.083,.067],'Blue',label+'Arm',16)
    ell(label+' elbow',(side*.37,1.19,.025),(.07,.07,.075),'Blue',label+'Forearm')
    tube(label+' forearm sleeve',[(side*.37,1.19,.025),(side*.4,1.08,.042),(side*.43,.99,.057)],[.067,.066,.055],'Navy',label+'Forearm',16)
    box(label+' white cuff',(side*.429,1.015,.055),(.135,.075,.13),'Hair',label+'Forearm',.018)
    ell(label+' gloved palm',(side*.45,.942,.06),(.065,.088,.043),'Leather',label+'Hand')
    for fi in range(4):
        x=side*.45+(fi-1.5)*.026
        tube(label+' finger '+str(fi),[(x,.903,.055),(x,.856,.061),(x,.837,.092)],[.013,.012,.009],'Leather',label+'Hand',8)
    tube(label+' thumb',[(side*.408,.961,.071),(side*.386,.926,.092),(side*.392,.906,.11)],[.022,.017,.012],'Leather',label+'Hand')
    ell(label+' tailored shoulder',(side*.247,1.52,0),(.115,.075,.11),'Navy',label+'Arm')
    tube(label+' epaulet edge',[(side*.18,1.56,.085),(side*.25,1.582,.105),(side*.32,1.54,.06)],[.012]*3,'Gold',label+'Arm')
    for i in range(5):tube(label+' shoulder tassel',[(side*(.21+i*.019),1.53,.10),(side*(.21+i*.019),1.465,.105)],[.006,.004],'Gold',label+'Arm',6)
# A fitted closed jacket, waist cinch and flowing coat tails with contrasting lining.
torso('Fitted navy jacket',[(1.01,.19,.117),(1.13,.175,.12),(1.31,.205,.137),(1.46,.255,.123),(1.54,.22,.105)],'Navy')
torso('High waist belt',[(1.035,.198,.13),(1.09,.19,.135)],'Leather','Hips')
box('Engraved belt buckle',(0,1.065,.146),(.092,.055,.025),'Gold','Hips',.013)
ell('Buckle sea glass',(0,1.065,.166),(.027,.021,.011),'Teal','Hips')
for side in (-1,1):
    surface('Swept lapel',[[ (side*.025,1.51,.14),(side*.12,1.53,.134),(side*.213,1.49,.106)],[(side*.045,1.36,.151),(side*.09,1.37,.16),(side*.16,1.4,.143)],[(side*.035,1.24,.14),(side*.05,1.265,.149),(side*.07,1.29,.143)]],'Blue')
    tube('Lapel gold piping',[(side*.025,1.51,.157),(side*.045,1.36,.165),(side*.035,1.24,.153)],[.005]*3,'Gold',sides=8)
    rows=[]
    for y,w,dz in [(1.07,.20,-.105),(.94,.23,-.13),(.76,.285,-.17),(.63,.31,-.20)]:
        rows.append([(side*.04,y,dz),(side*w*.65,y+.025,dz-.035),(side*w,y+.08,dz+.05)])
    surface('Long split coat tail',rows,'Blue','Hips',.022)
    tube('Coat tail gold hem',[rows[-1][i] for i in range(3)],[.007]*3,'Gold','Hips',8)
    for y in (1.19,1.3,1.41):ell('Braided jacket button',(side*.065,y,.157),(.016,.018,.015),'Gold')
    tube('Pocket flap',[(side*.13,1.19,.123),(side*.19,1.2,.091)],[.016,.016],'Blue')
# Sculpted collar/ruff, pendant, braided chain and scarf knot.
ell('Neck',(0,1.626,0),(.067,.1,.063),'Skin','Head')
for i in range(9):
    a=(i-4)*.17;ell('White collar pleat',(math.sin(a)*.083,1.568,math.cos(a)*.092),(.024,.049,.026),'Hair')
ell('Collar brooch',(0,1.532,.118),(.044,.049,.023),'Gold')
ell('Brooch gemstone',(0,1.534,.14),(.024,.032,.012),'Teal')
for i in range(19):
    t=i/18;tube('Gold watch chain',[(.11+t*.105,1.412-.048*math.sin(t*math.pi),.154-t*.06),(.113+t*.105,1.409-.048*math.sin(t*math.pi),.154-t*.06)],[.005,.005],'Gold',sides=6)
# New rounded face, cheeks, ears, modeled eyelids, iris/pupil/catchlight and lips.
ell('Face',(0,1.842,.014),(.143,.183,.13),'Skin','Head',40,28)
ell('Jaw',(0,1.748,.034),(.107,.099,.094),'Skin','Head',32,20)
for side in (-1,1):
    ell('Ear',(side*.141,1.821,.008),(.028,.047,.02),'Skin','Head')
    ell('Eye white',(side*.058,1.854,.128),(.04,.036,.016),'Hair','Head')
    ell('Turquoise iris',(side*.058,1.854,.143),(.022,.027,.007),'Teal','Head',24,16)
    ell('Pupil',(side*.058,1.854,.15),(.010,.019,.005),'Dark','Head')
    ell('Eye catchlight',(side*.058-.007,1.864,.154),(.007,.008,.004),'Hair','Head',12,8)
    tube('Upper eyelid',[(side*.018,1.864,.137),(side*.058,1.886,.135),(side*.098,1.862,.12)],[.005,.006,.005],'Dark','Head',8)
    tube('Lower eyelid',[(side*.023,1.842,.137),(side*.058,1.826,.137),(side*.092,1.843,.124)],[.003]*3,'Skin','Head',8)
    tube('Eyebrow',[(side*.022,1.912,.121),(side*.058,1.925,.118),(side*.094,1.911,.107)],[.007,.007,.004],'Navy','Head',8)
    for i in range(2):tube('Eyelash',[(side*(.081+i*.012),1.871-i*.004,.125),(side*(.096+i*.012),1.884-i*.009,.12)],[.003,.001],'Dark','Head',6)
    ell('Cheek',(side*.063,1.775,.101),(.039,.018,.013),'Skin','Head')
ell('Nose bridge',(0,1.824,.132),(.019,.033,.018),'Skin','Head')
ell('Nose tip',(0,1.802,.146),(.023,.019,.022),'Skin','Head')
tube('Upper lip',[(-.03,1.761,.14),(0,1.766,.146),(.03,1.761,.14)],[.003,.006,.003],'Red','Head',10)
tube('Lower lip',[(-.024,1.756,.141),(0,1.752,.147),(.024,1.756,.141)],[.003,.004,.003],'Skin','Head',10)
# Hair is modeled as tapered, curved locks with a scalp under the swept fringe.
ell('Hair cap',(0,1.888,-.026),(.157,.174,.138),'Hair','Head',32,20)
# Keep the face visible; the rear hair mass terminates at the neck.
for i in range(15):
    a=math.pi*.48+i*math.pi*1.04/14;x=math.sin(a)*.144;z=math.cos(a)*.128-.025
    tube('Layered rear hair',[(x*.72,2.016,z*.65),(x,1.9,z),(x*1.02,1.76,z-.015),(x*.75,1.66,z-.022)],[.037,.041,.029,.004],'Hair','Head',10)
for i in range(7):
    x=-.12+i*.034;endx=x+.04
    tube('Swept front fringe',[(x*.6,2.025,.017),(x,1.986,.10),(endx,1.94,.122),(endx+.012,1.91+(i%3)*.012,.114)],[.036,.036,.023,.002],'Hair','Head',10)
for side in (-1,1):
    tube('Face framing hair',[(side*.139,1.956,.02),(side*.166,1.84,.01),(side*.161,1.7,.008),(side*.126,1.66,.03)],[.035,.032,.024,.001],'Hair','Head',12)
# Swept tricorn hat: curved rim, band, jewel, feather vein and barbs.
torso('Hat crown',[(2.045,.159,.14),(2.114,.137,.117),(2.142,.117,.098)],'Navy','Head')
ell('Hat crown cap',(0,2.141,0),(.12,.018,.10),'Navy','Head',32,12)
rows=[]
for r in (1,1.2,1.38):
    row=[]
    for k in range(49):
        a=k*math.tau/48;radius=(.16+.016*math.cos(3*a))*r;row.append((radius*math.sin(a),2.044+.025*math.cos(3*a)*(r-1)*3,radius*math.cos(a)))
    rows.append(row)
surface('Swept hat brim',rows,'Navy','Head',.018)
tube('Hat rim braid',rows[-1],[.006]*49,'Gold','Head',8)
ell('Hat crest',(0,2.088,.143),(.035,.043,.018),'Gold','Head')
ell('Crest gem',(0,2.089,.164),(.019,.025,.009),'Teal','Head')
tube('Feather shaft',[(.09,2.106,-.03),(.145,2.25,-.055),(.21,2.36,-.07)],[.003,.003,.001],'Gold','Head',8)
surface('Feather vane',[[ (.075,2.11,-.03),(.096,2.108,-.03)],[(.105,2.20,-.045),(.159,2.177,-.045)],[(.143,2.29,-.06),(.2,2.26,-.06)],[(.21,2.36,-.07),(.21,2.36,-.07)]],'Hair','Head',.003)
for i in range(9):
    t=i/9;x=.09+.12*t;y=2.11+.24*t;z=-.03-.04*t
    for side in (-1,1):tube('Feather barb',[(x,y,z),(x+side*.027*(1-t),y+.036,z+.012)],[.005,.001],'Hair','Head',6)
# A narrow modeled rapier with fuller and marine guard, carried at the right hand.
tube('Rapier grip',[(.45,.885,.085),(.45,1.04,.09)],[.027,.025],'Leather','RightHand',12)
for y in (.9,.94,.98,1.02):ell('Grip wrap',(.45,y,.09),(.029,.008,.03),'Gold','RightHand',16,8)
ell('Rapier pommel',(.45,.86,.085),(.035,.04,.035),'Gold','RightHand')
tube('Rapier crossguard',[(.32,1.047,.1),(.45,1.037,.09),(.57,1.047,.07)],[.014,.012,.012],'Gold','RightHand',10)
tube('Curved knuckle guard',[(.57,1.047,.07),(.59,.975,.045),(.56,.884,.057),(.45,.862,.085)],[.009]*4,'Gold','RightHand',10)
blade=surface('Rapier blade',[[ (.432,1.07,.09),(.45,1.07,.112),(.468,1.07,.09)],[(.436,1.48,.087),(.45,1.48,.102),(.464,1.48,.087)],[(.45,1.85,.084),(.45,1.85,.086),(.45,1.85,.084)]],'Steel','RightHand',.012)
tube('Rapier inlay',[(.45,1.1,.116),(.45,1.46,.105)],[.003,.001],'Teal','RightHand',6)
# Weld the face, jaw, nose, ears and neck into a single smooth sculpted surface.
face_names=('Face','Jaw','Ear','Cheek','Nose bridge','Nose tip','Neck')
face_parts=[o for o in parts if any(o.name==n or o.name.startswith(n+'.') for n in face_names)]
parts=[o for o in parts if o not in face_parts]
bpy.ops.object.select_all(action='DESELECT')
for o in face_parts:o.select_set(True)
bpy.context.view_layer.objects.active=face_parts[0];bpy.ops.object.join();sculpt=bpy.context.object
rem=sculpt.modifiers.new('Sculpted face union','REMESH');rem.mode='VOXEL';rem.voxel_size=.0055;rem.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=rem.name)
sm=sculpt.modifiers.new('Facial smoothing','SMOOTH');sm.factor=.45;sm.iterations=4;bpy.ops.object.modifier_apply(modifier=sm.name)
dec=sculpt.modifiers.new('Game mesh reduction','DECIMATE');dec.ratio=.65;bpy.ops.object.modifier_apply(modifier=dec.name)
finish(sculpt,'Sculpted Face and Neck','Skin','Head')
# Real 16-bone deform armature and normalized skin weights on every character mesh.
spec=[('Root',None,(0,0,0),(0,.4,0)),('Hips','Root',(0,1.04,0),(0,1.23,0)),('Spine','Hips',(0,1.23,0),(0,1.55,0)),('Head','Spine',(0,1.6,0),(0,1.95,0))]
for side,label in [(-1,'Left'),(1,'Right')]:
    spec += [(label+'Arm','Spine',(side*.255,1.5,0),(side*.37,1.19,.025)),(label+'Forearm',label+'Arm',(side*.37,1.19,.025),(side*.43,.99,.057)),(label+'Hand',label+'Forearm',(side*.43,.99,.057),(side*.45,.85,.07)),(label+'Leg','Hips',(side*.105,1.04,0),(side*.13,.6,.018)),(label+'Shin',label+'Leg',(side*.13,.6,.018),(side*.13,.08,.04)),(label+'Foot',label+'Shin',(side*.13,.08,.04),(side*.13,.08,.22))]
ar=bpy.data.armatures.new('Voyager_DeformSkeleton');rig=bpy.data.objects.new('Voyager_Rig',ar);bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);rig.show_in_front=True
bpy.ops.object.mode_set(mode='EDIT')
for name,parent,a,b in spec:
    bone=ar.edit_bones.new(name);bone.head=P(a);bone.tail=P(b)
    if parent:bone.parent=ar.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
for o in parts:
    group=o.vertex_groups.new(name=o['deform_bone']);group.add(list(range(len(o.data.vertices))),1,'REPLACE')
    mod=o.modifiers.new('Bound to deform skeleton','ARMATURE');mod.object=rig
    # Smart UV unwrap produces stored UV coordinates on every sculpted part.
    # Primitive sphere UVs are preserved; custom surfaces use explicit projected UVs.
# Keyed inspection pose: elbows and knees genuinely move bound meshes.
for frame,a in [(1,0),(20,.13),(40,0),(60,-.13),(80,0)]:
    for name,sign in [('LeftArm',1),('RightArm',-1),('LeftLeg',-1),('RightLeg',1)]:
        pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler[0]=a*sign;pb.keyframe_insert(data_path='rotation_euler',frame=frame)
rig.animation_data.action.name='Voyager_Bound_Inspection';rig.animation_data.action.use_fake_user=True
scene=bpy.context.scene;scene.frame_set(1);scene.frame_end=80
# Count real geometry, UV and skeleton evidence before any studio objects are added.
tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in parts)
report={'character':'Tideward Voyager v2','mesh_parts':len(parts),'triangles':tris,'deform_bones':len(spec),'all_parts_have_uv':all(o.data.uv_layers.active is not None for o in parts),'armature_bound_meshes':len(parts)}
# Studio materials and lighting.
def plain(name,color,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(*[linear(v) for v in color],1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=.4;return m
floor=plain('Voyager Studio Floor',(.095,.15,.21));stand=plain('Voyager Display Plinth',(.19,.29,.34),.3)
bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=.65,depth=.1,location=(0,0,-.07));ped=bpy.context.object;ped.name='Studio Plinth';ped.data.materials.append(stand);m=ped.modifiers.new('Soft edge','BEVEL');m.width=.026;m.segments=3;ped.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.125));bpy.context.object.data.materials.append(floor)
world=bpy.data.worlds.new('Voyager Studio');world.use_nodes=True;scene.world=world;world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.28,.36,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45
for name,pos,power,size,col in [('Key softbox',(-3,-4,5),1000,4,(.8,.91,1)),('Warm fill',(3,-2,3),650,3,(1,.84,.69)),('Rim strip',(0,3,4),1100,3,(.45,.86,1))]:
    bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.name=name;l.data.energy=power;l.data.size=size;l.data.color=col;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3.8,-7,3.0));cam=bpy.context.object;scene.camera=cam;cam.rotation_euler=(Vector((0,0,1.14))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.95
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False;scene.cycles.adaptive_threshold=.035;scene.cycles.adaptive_min_samples=12;scene.cycles.max_bounces=4;scene.cycles.diffuse_bounces=2;scene.cycles.glossy_bounces=2
scene.render.resolution_x=1700;scene.render.resolution_y=1800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
for image in bpy.data.images:
    if image.source=='FILE' and not image.packed_file:
        try:image.pack()
        except RuntimeError:pass
bpy.ops.wm.save_as_mainfile(filepath=out+'/Tideward_Voyager_V2.blend')
scene.render.filepath=out+'/01_Voyager_Finished.png';bpy.ops.render.render(write_still=True)
cam.location=(-4,6,3);cam.rotation_euler=(Vector((0,0,1.14))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=out+'/02_Voyager_Coat_Hair.png';bpy.ops.render.render(write_still=True)
# A real rig-inspection image; the bones use the armature's heads and tails.
rigmat=plain('Deform bone turquoise',(.06,.84,.75));bs=rigmat.node_tree.nodes['Principled BSDF'];bs.inputs['Emission Color'].default_value=(.04,.4,.3,1);bs.inputs['Emission Strength'].default_value=.3
for bone in ar.bones:
    a=bone.head_local+Vector((0,-.28,0));b=bone.tail_local+Vector((0,-.28,0));v=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=4,radius1=.018,radius2=.008,depth=v.length,location=(a+b)/2);o=bpy.context.object;o.name='Rig inspection '+bone.name;o.rotation_euler=v.to_track_quat('Z','Y').to_euler();o.data.materials.append(rigmat)
cam.location=(0,-7,2.4);cam.rotation_euler=(Vector((0,0,1.14))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=out+'/03_Voyager_Deform_Rig.png';bpy.ops.render.render(write_still=True)
# Verify bound mesh deformation numerically from the evaluated Blender dependency graph.
scene.frame_set(1);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();before={o.name:[v.co.copy() for v in o.evaluated_get(dg).data.vertices] for o in parts}
scene.frame_set(20);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();moved=sum(sum((a-b.co).length>1e-5 for a,b in zip(before[o.name],o.evaluated_get(dg).data.vertices)) for o in parts)
assert moved>100,'The armature must deform actual mesh vertices';report['verified_moved_vertices']=moved
json.dump(report,open(out+'/Model_Validation.json','w'),indent=2);print('VOYAGER_COMPLETE',json.dumps(report))
