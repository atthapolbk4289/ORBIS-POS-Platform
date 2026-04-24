Add-Type -AssemblyName System.Data

$connStr = "Server=localhost;Database=PosDB;Integrated Security=True;TrustServerCertificate=True"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$branchId = [System.Guid]::Parse('44538349-F06D-4457-8049-5D47A1DC8A69')
$catFood   = [System.Guid]::Parse('8BFD462F-3ABC-4C54-B0F3-137924DE3B21')
$catDrink  = [System.Guid]::Parse('BDA7480E-B94F-490F-9BCA-B1FB6D550E15')
$catDessert= [System.Guid]::Parse('1A2885C7-B855-4FFC-90AC-2B6911D70337')
$catRetail = [System.Guid]::Parse('798DBF2D-2F29-42FA-BCD5-E41BD59804A3')

$products = @(
  @('FD001','ข้าวผัดหมู','Fried Rice Pork',80,35,'จาน','FOOD',$catFood,1),
  @('FD002','ข้าวผัดไก่','Fried Rice Chicken',80,35,'จาน','FOOD',$catFood,1),
  @('FD003','ข้าวผัดกุ้ง','Fried Rice Shrimp',100,50,'จาน','FOOD',$catFood,0),
  @('FD004','ผัดกระเพราหมู','Basil Pork',90,40,'จาน','FOOD',$catFood,1),
  @('FD005','ผัดกระเพราไก่','Basil Chicken',90,40,'จาน','FOOD',$catFood,0),
  @('FD006','ผัดกระเพรากุ้ง','Basil Shrimp',110,55,'จาน','FOOD',$catFood,0),
  @('FD007','ผัดไทยกุ้ง','Pad Thai Shrimp',120,55,'จาน','FOOD',$catFood,1),
  @('FD008','ผัดไทยหมู','Pad Thai Pork',100,45,'จาน','FOOD',$catFood,0),
  @('FD009','ต้มยำกุ้ง','Tom Yum Shrimp',150,70,'ชาม','FOOD',$catFood,1),
  @('FD010','ต้มยำหมู','Tom Yum Pork',120,55,'ชาม','FOOD',$catFood,0),
  @('FD011','ต้มข่าไก่','Tom Kha Chicken',130,60,'ชาม','FOOD',$catFood,0),
  @('FD012','แกงเขียวหวานไก่','Green Curry Chicken',120,55,'จาน','FOOD',$catFood,1),
  @('FD013','แกงมัสมั่นไก่','Massaman Curry',140,65,'จาน','FOOD',$catFood,0),
  @('FD014','ส้มตำไทย','Papaya Salad',70,30,'จาน','FOOD',$catFood,1),
  @('FD015','ยำวุ้นเส้น','Glass Noodle Salad',90,40,'จาน','FOOD',$catFood,0),
  @('FD016','ไก่ทอดกระเทียม','Garlic Fried Chicken',110,50,'จาน','FOOD',$catFood,1),
  @('FD017','หมูทอดกระเทียม','Garlic Fried Pork',100,45,'จาน','FOOD',$catFood,0),
  @('FD018','ปลานึ่งซีอิ๊ว','Steamed Fish Soy Sauce',180,90,'จาน','FOOD',$catFood,0),
  @('FD019','ข้าวหน้าเป็ด','Duck on Rice',110,50,'จาน','FOOD',$catFood,1),
  @('FD020','ข้าวมันไก่','Chicken Rice',80,35,'จาน','FOOD',$catFood,1),
  @('FD021','บะหมี่หมูแดง','BBQ Pork Noodle',80,35,'ชาม','FOOD',$catFood,0),
  @('FD022','เส้นใหญ่น้ำ','Rice Noodle Soup',70,30,'ชาม','FOOD',$catFood,0),
  @('FD023','ข้าวไข่เจียว','Omelette Rice',60,25,'จาน','FOOD',$catFood,0),
  @('FD024','หมูกระทะ','Thai BBQ Pork Set',299,150,'ชุด','FOOD',$catFood,1),
  @('FD025','สุกี้หมู','Suki Pork',199,100,'ชุด','FOOD',$catFood,0),
  @('DR001','ชาเย็น','Thai Iced Tea',40,12,'แก้ว','DRINK',$catDrink,1),
  @('DR002','กาแฟเย็น','Iced Coffee',45,15,'แก้ว','DRINK',$catDrink,1),
  @('DR003','โอเลี้ยง','Black Iced Coffee',35,10,'แก้ว','DRINK',$catDrink,0),
  @('DR004','มะนาวโซดา','Lemonade Soda',40,12,'แก้ว','DRINK',$catDrink,1),
  @('DR005','น้ำเปล่า','Water',15,3,'ขวด','DRINK',$catDrink,0),
  @('DR006','ชาเขียวเย็น','Iced Green Tea',45,14,'แก้ว','DRINK',$catDrink,1),
  @('DR007','น้ำมะพร้าว','Coconut Water',50,20,'ผล','DRINK',$catDrink,1),
  @('DR008','น้ำผลไม้ปั่น','Fresh Juice',60,20,'แก้ว','DRINK',$catDrink,0),
  @('DR009','โคโค่เย็น','Iced Cocoa',50,18,'แก้ว','DRINK',$catDrink,0),
  @('DR010','เบียร์สิงห์','Singha Beer',80,45,'ขวด','DRINK',$catDrink,0),
  @('DR011','เบียร์ช้าง','Chang Beer',75,42,'ขวด','DRINK',$catDrink,0),
  @('DR012','โค้ก','Coke',30,10,'กระป๋อง','DRINK',$catDrink,0),
  @('DR013','เปปซี่','Pepsi',30,10,'กระป๋อง','DRINK',$catDrink,0),
  @('DR014','สมูทตี้','Smoothie',70,25,'แก้ว','DRINK',$catDrink,0),
  @('DR015','นมสด','Fresh Milk',30,10,'แก้ว','DRINK',$catDrink,0),
  @('DS001','ข้าวเหนียวมะม่วง','Mango Sticky Rice',80,30,'จาน','DESSERT',$catDessert,1),
  @('DS002','บัวลอยน้ำขิง','Taro Balls',50,18,'ชาม','DESSERT',$catDessert,0),
  @('DS003','ไอศกรีมกะทิ','Coconut Ice Cream',60,22,'ถ้วย','DESSERT',$catDessert,1),
  @('DS004','เค้กช็อคโกแลต','Chocolate Cake',90,35,'ชิ้น','DESSERT',$catDessert,1),
  @('DS005','กล้วยบวชชี','Banana Coconut Milk',45,15,'ชาม','DESSERT',$catDessert,0),
  @('RT001','มาม่าหมู','Mama Pork Noodle',7,4,'ซอง','RETAIL',$catRetail,0),
  @('RT002','มาม่าต้มยำกุ้ง','Mama Tom Yum',7,4,'ซอง','RETAIL',$catRetail,0),
  @('RT003','ข้าวสารหอมมะลิ 5 กก.','Jasmine Rice 5kg',250,180,'ถุง','RETAIL',$catRetail,1),
  @('RT004','น้ำมันพืช 1 ลิตร','Vegetable Oil 1L',65,45,'ขวด','RETAIL',$catRetail,0),
  @('RT005','ซีอิ๊วขาว 700ml','Soy Sauce 700ml',52,35,'ขวด','RETAIL',$catRetail,0),
  @('RT006','น้ำปลา 700ml','Fish Sauce 700ml',48,32,'ขวด','RETAIL',$catRetail,0),
  @('RT007','ไข่ไก่ (แผง 30 ฟอง)','Eggs 30pcs',130,95,'แผง','RETAIL',$catRetail,1),
  @('RT008','นมโฟร์โมสต์ 1 ลิตร','Foremost Milk 1L',52,38,'กล่อง','RETAIL',$catRetail,0),
  @('RT009','น้ำตาลทราย 1 กก.','Sugar 1kg',25,18,'ถุง','RETAIL',$catRetail,0),
  @('RT010','สบู่ก้อน','Soap Bar',28,18,'ก้อน','RETAIL',$catRetail,0),
  @('RT011','แชมพู 180ml','Shampoo 180ml',55,35,'ขวด','RETAIL',$catRetail,0),
  @('RT012','ยาสีฟัน 160g','Toothpaste 160g',48,32,'หลอด','RETAIL',$catRetail,0),
  @('RT013','กระดาษทิชชู่ 3 ม้วน','Tissue 3 Rolls',55,38,'แพ็ก','RETAIL',$catRetail,0),
  @('RT014','น้ำยาล้างจาน 500ml','Dish Soap 500ml',38,25,'ขวด','RETAIL',$catRetail,0),
  @('RT015','ผงซักฟอก 900g','Detergent 900g',75,52,'ถุง','RETAIL',$catRetail,1)
)

$count = 0
foreach ($row in $products) {
  $pGuid = [System.Guid]::NewGuid()
  $sGuid = [System.Guid]::NewGuid()
  $sku=$row[0]; $name=$row[1]; $en=$row[2]; $price=[decimal]$row[3]; $cost=[decimal]$row[4]
  $unit=$row[5]; $type=$row[6]; $cat=[System.Guid]$row[7]; $feat=[int]$row[8]
  $stock = if($type -eq 'FOOD'){50} elseif($type -eq 'DRINK'){80} elseif($type -eq 'DESSERT'){30} else {200}
  $alert = if($type -eq 'FOOD'){10} elseif($type -eq 'DRINK'){20} elseif($type -eq 'DESSERT'){5} else {30}

  $cmd = $conn.CreateCommand()
  $cmd.CommandText = @"
INSERT INTO Products(Id,BranchId,CategoryId,Sku,Barcode,Name,NameEn,Price,CostPrice,Unit,ProductType,IsActive,IsFeatured,Taxable,CreatedAt,UpdatedAt)
VALUES(@Id,@BranchId,@CatId,@Sku,@Bar,@Name,@En,@Price,@Cost,@Unit,@Type,1,@Feat,0,GETUTCDATE(),GETUTCDATE())
"@
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Id", [System.Data.SqlDbType]::UniqueIdentifier))).Value = $pGuid
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@BranchId", [System.Data.SqlDbType]::UniqueIdentifier))).Value = $branchId
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@CatId", [System.Data.SqlDbType]::UniqueIdentifier))).Value = $cat
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Sku", [System.Data.SqlDbType]::NVarChar, 50))).Value = $sku
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Bar", [System.Data.SqlDbType]::NVarChar, 50))).Value = "885$sku"
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Name", [System.Data.SqlDbType]::NVarChar, 200))).Value = $name
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@En", [System.Data.SqlDbType]::NVarChar, 200))).Value = $en
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Price", [System.Data.SqlDbType]::Decimal))).Value = $price
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Cost", [System.Data.SqlDbType]::Decimal))).Value = $cost
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Unit", [System.Data.SqlDbType]::NVarChar, 50))).Value = $unit
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Type", [System.Data.SqlDbType]::NVarChar, 50))).Value = $type
  $cmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Feat", [System.Data.SqlDbType]::Bit))).Value = $feat
  $cmd.ExecuteNonQuery() | Out-Null

  $cmd2 = $conn.CreateCommand()
  $cmd2.CommandText = "INSERT INTO ProductStocks(Id,BranchId,ProductId,PhysicalQty,ReservedQty,MinAlertQty,UpdatedAt) VALUES(@Id,@BranchId,@PId,@Qty,0,@Alert,GETUTCDATE())"
  $cmd2.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Id",[System.Data.SqlDbType]::UniqueIdentifier))).Value = $sGuid
  $cmd2.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@BranchId",[System.Data.SqlDbType]::UniqueIdentifier))).Value = $branchId
  $cmd2.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@PId",[System.Data.SqlDbType]::UniqueIdentifier))).Value = $pGuid
  $cmd2.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Qty",[System.Data.SqlDbType]::Int))).Value = $stock
  $cmd2.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Alert",[System.Data.SqlDbType]::Int))).Value = $alert
  $cmd2.ExecuteNonQuery() | Out-Null
  $count++
}

$customers = @(
  @('สมชาย ใจดี','0812345678','somchai@email.com',1250,12500,'GOLD'),
  @('วิไล สุขใจ','0823456789','wilai@email.com',850,8500,'SILVER'),
  @('ประเสริฐ มั่งมี','0834567890','',320,3200,'BRONZE'),
  @('นิภา รักดี','0845678901','nipa@email.com',2100,21000,'PLATINUM'),
  @('อนันต์ สมบูรณ์','0856789012','',0,0,'NORMAL'),
  @('พรทิพย์ แก้วใส','0867890123','porntip@email.com',430,4300,'BRONZE'),
  @('สุชาติ ทองดี','0878901234','',1580,15800,'GOLD'),
  @('มาลี วงศ์งาม','0889012345','malee@email.com',720,7200,'SILVER'),
  @('วิชัย ศรีสวัสดิ์','0890123456','',100,1000,'NORMAL'),
  @('กนกพร ดาวเรือง','0801234567','kanokporn@email.com',3200,32000,'PLATINUM'),
  @('ธนภัทร จันทร์เพ็ญ','0812222333','',560,5600,'SILVER'),
  @('อรอุมา ไพรสน','0823333444','ornuma@email.com',890,8900,'SILVER'),
  @('ณัฐพล รุ่งเรือง','0834444555','',210,2100,'BRONZE'),
  @('ปิยนันท์ สายใจ','0845555666','piyanun@email.com',4500,45000,'PLATINUM'),
  @('สิริมา บุญมา','0856666777','',0,450,'NORMAL')
)
foreach ($c in $customers) {
  $cGuid = [System.Guid]::NewGuid()
  $cc = $conn.CreateCommand()
  $cc.CommandText = "INSERT INTO Customers(Id,BranchId,Name,Phone,Email,Points,TotalSpent,MemberLevel,IsActive,CreatedAt) VALUES(@Id,@B,@N,@Ph,@Em,@Pts,@Spent,@Lv,1,GETUTCDATE())"
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Id",[System.Data.SqlDbType]::UniqueIdentifier))).Value = $cGuid
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@B",[System.Data.SqlDbType]::UniqueIdentifier))).Value = $branchId
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@N",[System.Data.SqlDbType]::NVarChar,200))).Value = $c[0]
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Ph",[System.Data.SqlDbType]::NVarChar,20))).Value = $c[1]
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Em",[System.Data.SqlDbType]::NVarChar,200))).Value = $c[2]
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Pts",[System.Data.SqlDbType]::Int))).Value = [int]$c[3]
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Spent",[System.Data.SqlDbType]::Decimal))).Value = [decimal]$c[4]
  $cc.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@Lv",[System.Data.SqlDbType]::NVarChar,20))).Value = $c[5]
  $cc.ExecuteNonQuery() | Out-Null
}
$conn.Close()
Write-Host "SUCCESS: $count products + $($customers.Count) customers"
