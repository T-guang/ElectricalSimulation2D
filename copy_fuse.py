import os
import shutil

src_1p = r"temp_fuse\熔断器(FU)\单极熔断器\单极熔断器.png"
dst_1p = r"Assets\Art\Components\Fuse_1P_Visual.png"
shutil.copyfile(src_1p, dst_1p)

src_3p = r"temp_fuse\熔断器(FU)\三相熔断器\三相熔断器.png"
dst_3p = r"Assets\Art\Components\Fuse_3P_Visual.png"
shutil.copyfile(src_3p, dst_3p)
print("Copied images.")
