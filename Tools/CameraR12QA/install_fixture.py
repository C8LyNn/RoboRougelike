import pathlib,shutil,uuid
r=pathlib.Path(__file__).resolve().parents[2]
old=r.parent/'RoboRougelike-W9-Validation/Assets/CameraNativeQA'
dest=r/'Assets/CameraR12NativeQA';(dest/'Editor').mkdir(parents=True,exist_ok=True)
for name,target in [('CameraR12NativeQA.cs',dest/'CameraR12NativeQA.cs'),('CameraR12QABuild.cs',dest/'Editor/CameraR12QABuild.cs')]:
 shutil.copyfile(r/'Tools/CameraR12QA'/name,target)
shutil.copyfile(old/'Template.mat',dest/'Template.mat')
# Freeze a scene asset/meta ahead of Build, which saves the same receiver-only scene.
(dest/'CAMERA_R12_QA.unity').write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n')
for p in [dest,dest/'Editor',dest/'CameraR12NativeQA.cs',dest/'Editor/CameraR12QABuild.cs',dest/'Template.mat',dest/'CAMERA_R12_QA.unity']:
 guid=uuid.uuid5(uuid.NAMESPACE_URL,'rrl-camera-r12-native-qa/'+str(p.relative_to(r))).hex
 text='fileFormatVersion: 2\nguid: '+guid+'\n'
 if p.is_dir():text+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 elif p.suffix=='.cs':text+='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 elif p.suffix=='.mat':text+='NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 else:text+='DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
 pathlib.Path(str(p)+'.meta').write_text(text)
print('Installed receiver fixture only; producer CAMERA preserved')
