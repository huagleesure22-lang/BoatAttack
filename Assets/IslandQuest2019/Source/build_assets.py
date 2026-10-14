"""Original low-poly island assets. Blender 4.3; exported geometry is imported by Unity 2019.
Run: blender -b --python build_assets.py -- OUTPUT_DIRECTORY
"""
import bpy, math, json, sys, os
from mathutils import Vector, Matrix

out = sys.argv[sys.argv.index('--') + 1]
os.makedirs(out, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
C = Matrix(((1,0,0),(0,0,-1),(0,1,0)))  # Unity x,y,z -> Blender x,-z,y
palette = {
    'Navy': ('15304C', .2, .42), 'Blue': ('287EAE', .22, .35),
    'Gold': ('D8A74B', .72, .3), 'Steel': ('B8D5DE', .65, .27),
    'Leather': ('30272C', .12, .62), 'Skin': ('F2C4A0', 0, .62),
    'Hair': ('D7E5E4', .1, .45), 'Bone': ('E8D7B3', .05, .58),
    'Dark': ('17272E', .1, .63), 'Wood': ('593626', 0, .7),
    'WoodLight': ('A26C3C', 0, .6), 'Teal': ('46D3D0', .3, .3),
    'Red': ('B25141', .15, .5), 'Stone': ('3D5663', .12, .65)
}
materials={}
def rgb(s): return tuple(int(s[i:i+2],16)/255 for i in (0,2,4))
def linear(c): return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c)
for name,(hexcol,metal,rough) in palette.items():
    mat=bpy.data.materials.new(name); mat.diffuse_color=(*rgb(hexcol),1); mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*linear(rgb(hexcol)),1)
    bs.inputs['Metallic'].default_value=metal; bs.inputs['Roughness'].default_value=rough
    if name=='Teal': bs.inputs['Emission Color'].default_value=mat.diffuse_color; bs.inputs['Emission Strength'].default_value=.35
    materials[name]=mat

def finish(obj,name,pos,mat,parent=None):
    obj.name=name
    if parent: obj.parent=parent
    obj.location=C@Vector(pos)
    if mat: obj.data.materials.append(materials[mat])
    return obj
def pivot(name,pos,parent=None):
    o=bpy.data.objects.new(name,None); bpy.context.collection.objects.link(o); return finish(o,name,pos,None,parent)
def box(name,pos,size,mat,parent=None,bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1); o=bpy.context.object
    o.scale=(size[0],size[2],size[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('CraftedEdges','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o,name,pos,mat,parent)
def ell(name,pos,size,mat,parent=None,sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1); o=bpy.context.object
    o.scale=(size[0],size[2],size[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,pos,mat,parent)
def cone(name,pos,r1,r2,h,mat,parent=None,verts=8):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=h)
    return finish(bpy.context.object,name,pos,mat,parent)
def bone(name,a,b,r,mat,parent=None):
    av=C@Vector(a); bv=C@Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=r,radius2=r*.8,depth=(bv-av).length)
    o=bpy.context.object; o.rotation_mode='QUATERNION'; o.rotation_quaternion=(bv-av).to_track_quat('Z','Y')
    mid=(Vector(a)+Vector(b))*.5
    return finish(o,name,mid,mat,parent)
def mesh(name,pos,vertices,faces,mat,parent=None):
    me=bpy.data.meshes.new(name); me.from_pydata([C@Vector(v) for v in vertices],[],faces); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o); return finish(o,name,pos,mat,parent)

def sword(name,tier=0):
    root=pivot(name,(0,0,0)); metal=['Steel','Teal','Gold'][tier]
    # Diamond cross-section and deliberately modeled cutting edge.
    v=[(-.075,.22,0),(0,.22,.045),(.075,.22,0),(0,.22,-.045),
       (-.065,.98,0),(0,.98,.035),(.065,.98,0),(0,.98,-.035),(0,1.2,0)]
    f=[(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),
       (4,5,8),(5,6,8),(6,7,8),(7,4,8)]
    mesh('Blade',(0,0,0),v,f,metal,root)
    box('Crossguard',(0,.21,0),(.36,.06,.12),'Gold',root)
    for side in (-1,1): ell('GuardTip'+str(side),(side*.17,.24,0),(.05,.07,.055),'Gold',root)
    cone('Grip',(0,.07,0),.037,.034,.22,'Leather',root)
    for y in (-.015,.04,.09,.14): cone('GripRing'+str(y),(0,y,0),.039,.039,.018,'Gold',root)
    ell('Pommel',(0,-.07,0),(.06,.06,.05),'Gold',root)
    ell('CoreGem',(0,.22,.067),(.045,.06,.025),'Teal',root)
    if tier>0:
        for y in (.4,.53,.66,.79): mesh('Rune'+str(y),(0,y,.047),[(-.022,0,0),(0,.035,0),(.022,0,0),(0,-.035,0)],[(0,1,2,3)],'Teal',root)
    if tier==2:
        for side in (-1,1): mesh('Wing'+str(side),(0,.25,0),[(side*.09,0,-.01),(side*.25,.18,-.01),(side*.18,.2,-.01),(side*.06,.06,-.01)],[(0,1,2,3)],'Gold',root)
    return root

hero=pivot('Hero',(0,0,0))
for s,label in [(-1,'Left'),(1,'Right')]:
    leg=pivot(label+'Leg',(s*.145,.91,0),hero)
    bone('Trouser',(0,0,0),(0,-.38,0),.105,'Navy',leg)
    box('Boot',(0,-.61,.045),(.22,.45,.3),'Leather',leg,.035)
    box('BootCuff',(0,-.41,0),(.23,.09,.22),'Gold',leg)
    box('BootToe',(0,-.78,.1),(.23,.11,.33),'Navy',leg)
    arm=pivot(label+'Arm',(s*.33,1.49,0),hero)
    ell('Shoulder',(0,0,0),(.17,.14,.17),'Gold',arm,2)
    bone('Sleeve',(0,-.06,0),(s*.07,-.43,.025),.105,'Blue',arm)
    box('Cuff',(s*.07,-.4,.025),(.16,.09,.18),'Hair',arm)
    ell('Hand',(s*.07,-.5,.045),(.085,.11,.075),'Skin',arm,2)
cone('Coat',(0,1.21,0),.28,.34,.66,'Navy',hero)
box('Vest',(0,1.28,.31),(.27,.42,.09),'Blue',hero)
box('Belt',(0,.97,0),(.63,.1,.59),'Leather',hero)
box('Buckle',(0,.97,.315),(.14,.1,.035),'Gold',hero)
for y in (1.13,1.26,1.39): ell('Button'+str(y),(0,y,.37),(.024,.024,.018),'Gold',hero)
for s in (-1,1):
    mesh('Lapel'+str(s),(0,0,0),[(s*.03,1.5,.36),(s*.27,1.52,.23),(s*.16,1.19,.33)],[(0,1,2)],'Gold',hero)
cone('Neck',(0,1.65,0),.075,.075,.16,'Skin',hero)
ell('Head',(0,1.84,0),(.205,.24,.175),'Skin',hero,2)
ell('HairBack',(0,1.88,-.075),(.23,.25,.15),'Hair',hero,2)
for s in (-1,1):
    ell('HairSide'+str(s),(s*.18,1.82,.02),(.065,.21,.08),'Hair',hero)
    box('Eye'+str(s),(s*.075,1.86,.165),(.035,.048,.025),'Dark',hero,.007)
    box('Eyebrow'+str(s),(s*.07,1.925,.15),(.075,.016,.024),'Hair',hero,.005)
ell('Nose',(0,1.81,.179),(.035,.045,.035),'Skin',hero)
box('Mouth',(0,1.745,.16),(.065,.012,.018),'Wood',hero,.003)
cone('HatCrown',(0,2.025,-.018),.235,.19,.16,'Navy',hero)
mesh('HatBrim',(0,2.0,0),[(-.34,0,.13),(0,.06,.3),(.34,0,.13),(.27,.09,-.21),(0,.03,-.29),(-.27,.09,-.21)],[(0,1,2,3,4,5)],'Navy',hero)
bone('HatTrimL',(-.34,2,.13),(0,2.06,.3),.015,'Gold',hero)
bone('HatTrimR',(0,2.06,.3),(.34,2,.13),.015,'Gold',hero)
ell('HatBadge',(0,2.04,.245),(.05,.055,.025),'Teal',hero)
mesh('Feather',(.17,2.09,-.01),[(0,0,0),(.035,.28,-.025),(.1,.35,-.04),(.12,.16,-.01)],[(0,1,2,3)],'Hair',hero)
# A modeled, thick cape; its folds stay visible without cloth simulation.
mesh('Cape',(0,0,0),[(-.28,1.55,-.15),(0,1.57,-.28),(.28,1.55,-.15),(-.4,.74,-.26),(0,.65,-.39),(.4,.74,-.26),
    (-.28,1.55,-.17),(0,1.57,-.30),(.28,1.55,-.17),(-.4,.74,-.28),(0,.65,-.41),(.4,.74,-.28)],
    [(0,3,4,1),(1,4,5,2),(6,7,10,9),(7,8,11,10),(0,1,7,6),(1,2,8,7),(3,9,10,4),(4,10,11,5),(0,6,9,3),(2,5,11,8)],'Blue',hero)
weapon=pivot('WeaponMount',(0,-.52,.065),bpy.data.objects['RightArm'])
blade=sword('HeldSword',0); blade.parent=weapon; blade.location=(0,0,0)
# Local weapon mesh is tilted by the game animation; preview is relaxed.
weapon.rotation_euler=(math.radians(-18),0,0)

sk=pivot('Skeleton',(0,0,0))
for s,label in [(-1,'Left'),(1,'Right')]:
    leg=pivot(label+'Leg',(s*.135,.87,0),sk)
    bone('Femur',(0,0,0),(s*.018,-.38,0),.057,'Bone',leg)
    ell('Knee',(s*.018,-.39,0),(.074,.065,.07),'Bone',leg)
    for k in (-1,1): bone('Shin'+str(k),(s*.018+k*.023,-.42,0),(k*.023,-.78,.035),.025,'Bone',leg)
    box('Foot',(0,-.83,.09),(.13,.08,.24),'Bone',leg,.018)
    for k in (-1,0,1): box('Toe'+str(k),(k*.045,-.83,.19),(.03,.065,.11),'Bone',leg,.01)
    arm=pivot(label+'Arm',(s*.26,1.4,0),sk)
    ell('Shoulder',(0,0,0),(.085,.08,.09),'Bone',arm)
    bone('UpperArm',(0,-.02,0),(s*.08,-.27,.01),.043,'Bone',arm)
    ell('Elbow',(s*.08,-.3,.02),(.055,.055,.055),'Bone',arm)
    for k in (-1,1): bone('Forearm'+str(k),(s*.08+k*.018,-.33,.02),(s*.11+k*.018,-.58,.05),.022,'Bone',arm)
    box('Palm',(s*.11,-.63,.05),(.1,.1,.065),'Bone',arm,.015)
    for k in (-1,0,1): bone('Finger'+str(k),(s*.11+k*.035,-.65,.07),(s*.11+k*.035,-.74,.09),.016,'Bone',arm)
ell('Pelvis',(0,.88,0),(.24,.13,.12),'Bone',sk)
box('PelvisHole',(0,.87,.115),(.09,.085,.025),'Dark',sk,.01)
for y in (.98,1.06,1.14,1.22,1.3,1.38,1.46): ell('Vertebra'+str(y),(0,y,-.05),(.063,.047,.065),'Bone',sk)
bone('Sternum',(0,1.13,.12),(0,1.42,.12),.029,'Bone',sk)
for y,width in [(1.16,.18),(1.25,.23),(1.34,.255),(1.43,.23)]:
    for s in (-1,1):
        pts=[(0,y,-.05),(s*width*.8,y+.045,-.09),(s*width,y,0),(s*width*.72,y-.035,.125),(s*.03,y-.04,.145)]
        for i in range(4): bone('Rib'+str(y)+str(s)+str(i),pts[i],pts[i+1],.021,'Bone',sk)
bone('Clavicle',(-.26,1.44,0),(.26,1.44,0),.033,'Bone',sk)
cone('Neck',(0,1.55,0),.045,.045,.15,'Bone',sk)
ell('Skull',(0,1.77,0),(.195,.215,.16),'Bone',sk,2)
for s in (-1,1):
    ell('Socket'+str(s),(s*.083,1.8,.144),(.066,.071,.032),'Dark',sk,2)
    ell('EyeGlow'+str(s),(s*.083,1.8,.17),(.022,.024,.01),'Teal',sk)
mesh('NoseHole',(0,1.71,.158),[(-.025,0,0),(0,.055,0),(.025,0,0)],[(0,1,2)],'Dark',sk)
box('Jaw',(0,1.6,.04),(.23,.075,.18),'Bone',sk,.025)
for x in (-.08,-.04,0,.04,.08): box('Tooth'+str(x),(x,1.655,.144),(.027,.04,.023),'Bone',sk,.003)
box('TatteredBelt',(0,.92,0),(.42,.07,.24),'Red',sk)
weapon2=pivot('WeaponMount',(.11,-.66,.08),bpy.data.objects.get('RightArm.001'))
rust=sword('SkeletonSword',0); rust.parent=weapon2; rust.location=(0,0,0)
weapon2.rotation_euler=(math.radians(-25),0,0)

ch=pivot('Chest',(0,0,0))
box('Base',(0,.32,0),(1.2,.6,.72),'Wood',ch,.045)
for z in (-.37,.37):
    for x in (-.45,-.225,0,.225,.45): box('Board'+str(z)+str(x),(x,.33,z),(.213,.48,.035),'WoodLight',ch,.007)
for x in (-.51,.51):
    box('IronCorner'+str(x),(x,.33,.397),(.08,.62,.045),'Gold',ch)
    box('Foot'+str(x),(x,.045,0),(.16,.09,.8),'Leather',ch)
box('BottomRim',(0,.075,0),(1.24,.065,.76),'Gold',ch)
lid=pivot('LidPivot',(0,.61,-.36),ch)
# Dome lid segmented into eight planks; hinge remains a real transform.
for i in range(8):
    a=math.pi*i/8; b=math.pi*(i+1)/8
    y1=.31*math.sin(a); z1=.36+.36*math.cos(a)
    y2=.31*math.sin(b); z2=.36+.36*math.cos(b)
    mesh('LidPlank'+str(i),(0,0,0),[(-.6,y1,z1),(.6,y1,z1),(.6,y2,z2),(-.6,y2,z2),(-.6,y1-.035,z1),(.6,y1-.035,z1),(.6,y2-.035,z2),(-.6,y2-.035,z2)],
        [(0,1,2,3),(7,6,5,4),(0,4,5,1),(3,2,6,7),(0,3,7,4),(1,5,6,2)],'WoodLight' if i%2 else 'Wood',lid)
    for x in (-.46,.46): bone('LidBand'+str(i)+str(x),(x,y1+.014,z1),(x,y2+.014,z2),.027,'Gold',lid)
for x in (-.602,.602):
    v=[(x,0,.36)]+[(x,.31*math.sin(math.pi*i/8),.36+.36*math.cos(math.pi*i/8)) for i in range(9)]
    mesh('LidEnd'+str(x),(0,0,0),v,[(0,i+1,i+2) for i in range(8)],'Wood',lid)
box('Lock',(0,.53,.411),(.19,.23,.08),'Gold',ch)
ell('LockGem',(0,.56,.461),(.061,.073,.022),'Teal',ch)
box('Keyhole',(0,.46,.457),(.027,.037,.015),'Dark',ch,.004)
for x in (-.48,.48):
    for y in (.17,.49): ell('Rivet'+str(x)+str(y),(x,y,.428),(.02,.02,.012),'Steel',ch)
for x in (-.38,.38): cone('Hinge'+str(x),(x,.61,-.385),.048,.048,.13,'Gold',ch)

swords=[sword('Sword'+str(i+1),i) for i in range(3)]
roots=[hero,sk,ch]+swords
def descendants(root):
    result=[root]
    for child in root.children: result+=descendants(child)
    return result
def export(root,filename):
    objects=[]
    for o in descendants(root):
        local=C.inverted()@o.matrix_local.to_3x3()@C
        q=local.to_quaternion(); pos=C.inverted()@o.location
        item={'name':o.name.split('.')[0], 'parent':-1 if o==root else descendants(root).index(o.parent),
              'position':list(pos),'rotation':[q.x,q.y,q.z,q.w], 'vertices':[], 'triangles':[], 'material':''}
        if o.type=='MESH':
            me=o.data; me.calc_loop_triangles()
            # Separate face vertices for authentic low-poly facets.
            for tri in me.loop_triangles:
                base=len(item['vertices'])//3
                for vi in tri.vertices: item['vertices']+=list(C.inverted()@me.vertices[vi].co)
                item['triangles'] += [base,base+1,base+2]
            item['material']=o.data.materials[0].name
        objects.append(item)
    data={'name':filename,'materials':[{'name':n,'color':[*rgb(v[0]),1],'metallic':v[1],'roughness':v[2]} for n,v in palette.items()], 'objects':objects}
    with open(os.path.join(out,filename+'.json'),'w') as f: json.dump(data,f,separators=(',',':'))
for root,name in zip(roots,['Hero','Skeleton','Chest','Sword1','Sword2','Sword3']): export(root,name)

# A real model render for inspection, using the same exported meshes.
hero.location=C@Vector((-1.7,0,0)); sk.location=C@Vector((1.5,0,0)); ch.location=C@Vector((.0,0,1.05))
for i,s in enumerate(swords): s.location=C@Vector((-.48+i*.48,.18,1.8))
for root in roots:
    if root in swords: root.rotation_euler=(math.radians(24),0,math.radians(-12))
box('Stage',(0,-.12,.6),(6,.2,4),'Stone',None,.08)
for x in (-1.7,1.5): cone('Pedestal',(x,-.015,0),.65,.65,.12,'Navy',None,32)
bpy.ops.object.camera_add(location=(6,-9,5)); cam=bpy.context.object
cam.rotation_euler=(Vector((0,-.3,1.1))-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='ORTHO';cam.data.ortho_scale=7.2
bpy.context.scene.camera=cam
for loc,power,size,col in [((-4,-5,7),1500,5,(.7,.87,1)),((5,-1,5),1200,4,(1,.78,.49)),((0,4,5),1800,3,(.4,1,1))]:
    bpy.ops.object.light_add(type='AREA',location=loc); l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.data.color=col
    l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=96;scene.cycles.use_denoising=False
scene.world.color=(.12,.12,.12);scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=os.path.join(out,'Assets_Overview.png')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'IslandQuest_OriginalAssets.blend'))
bpy.ops.render.render(write_still=True)
print('EXPORTED six original assets and overview render')
