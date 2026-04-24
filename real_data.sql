USE PosDB;
GO

-- ลบข้อมูลทดสอบเก่า
DELETE FROM ProductStocks;
DELETE FROM Products;
DELETE FROM Customers;
GO

DECLARE @BranchId UNIQUEIDENTIFIER = '44538349-F06D-4457-8049-5D47A1DC8A69';
DECLARE @CatFood UNIQUEIDENTIFIER = '8BFD462F-3ABC-4C54-B0F3-137924DE3B21';
DECLARE @CatDessert UNIQUEIDENTIFIER = '1A2885C7-B855-4FFC-90AC-2B6911D70337';
DECLARE @CatDrink UNIQUEIDENTIFIER = 'BDA7480E-B94F-490F-9BCA-B1FB6D550E15';
DECLARE @CatRetail UNIQUEIDENTIFIER = '798DBF2D-2F29-42FA-BCD5-E41BD59804A3';

-- ========== หมวด อาหาร ==========
INSERT INTO Products (Id,BranchId,CategoryId,Sku,Barcode,Name,NameEn,Price,CostPrice,Unit,ProductType,IsActive,IsFeatured,Taxable,CreatedAt,UpdatedAt)
VALUES
(NEWID(),@BranchId,@CatFood,'FD001','8850001001','ข้าวผัดหมู','Fried Rice with Pork',80,35,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD002','8850001002','ข้าวผัดไก่','Fried Rice with Chicken',80,35,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD003','8850001003','ข้าวผัดกุ้ง','Fried Rice with Shrimp',100,50,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD004','8850001004','ผัดกระเพราหมู','Basil Stir-fry Pork',90,40,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD005','8850001005','ผัดกระเพราไก่','Basil Stir-fry Chicken',90,40,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD006','8850001006','ผัดกระเพรากุ้ง','Basil Stir-fry Shrimp',110,55,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD007','8850001007','ผัดซีอิ๊วหมู','Soy Sauce Noodles Pork',80,35,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD008','8850001008','ผัดซีอิ๊วไก่','Soy Sauce Noodles Chicken',80,35,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD009','8850001009','ราดหน้าหมู','Gravy Noodles Pork',90,40,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD010','8850001010','ราดหน้าไก่','Gravy Noodles Chicken',90,40,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD011','8850001011','ผัดไทยกุ้ง','Pad Thai with Shrimp',120,55,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD012','8850001012','ผัดไทยหมู','Pad Thai with Pork',100,45,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD013','8850001013','ต้มยำกุ้ง','Tom Yum Shrimp',150,70,'ชาม','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD014','8850001014','ต้มยำหมู','Tom Yum Pork',120,55,'ชาม','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD015','8850001015','ต้มข่าไก่','Tom Kha Chicken',130,60,'ชาม','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD016','8850001016','แกงเขียวหวานไก่','Green Curry Chicken',120,55,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD017','8850001017','แกงเผ็ดเป็ด','Red Curry Duck',150,70,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD018','8850001018','แกงมัสมั่นไก่','Massaman Curry Chicken',140,65,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD019','8850001019','ส้มตำไทย','Papaya Salad Thai',70,30,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD020','8850001020','ส้มตำปูปลาร้า','Papaya Salad Crab',90,40,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD021','8850001021','ยำวุ้นเส้น','Glass Noodle Salad',90,40,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD022','8850001022','ยำทะเล','Seafood Salad',130,60,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD023','8850001023','ไก่ทอดกระเทียม','Garlic Fried Chicken',110,50,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD024','8850001024','หมูทอดกระเทียม','Garlic Fried Pork',100,45,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD025','8850001025','ปลานึ่งซีอิ๊ว','Steamed Fish Soy Sauce',180,90,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD026','8850001026','กุ้งผัดซอสมะขาม','Shrimp Tamarind Sauce',140,65,'จาน','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD027','8850001027','เส้นเล็กน้ำใส','Rice Noodle Soup Clear',70,30,'ชาม','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD028','8850001028','บะหมี่หมูแดง','Egg Noodle BBQ Pork',80,35,'ชาม','FOOD',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD029','8850001029','ข้าวหน้าเป็ด','Duck on Rice',110,50,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatFood,'FD030','8850001030','ข้าวมันไก่','Chicken Rice',80,35,'จาน','FOOD',1,1,0,GETUTCDATE(),GETUTCDATE());

-- ========== หมวด เครื่องดื่ม ==========
INSERT INTO Products (Id,BranchId,CategoryId,Sku,Barcode,Name,NameEn,Price,CostPrice,Unit,ProductType,IsActive,IsFeatured,Taxable,CreatedAt,UpdatedAt)
VALUES
(NEWID(),@BranchId,@CatDrink,'DR001','8850002001','ชาเย็น','Thai Iced Tea',40,12,'แก้ว','DRINK',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR002','8850002002','กาแฟเย็น','Iced Coffee',45,15,'แก้ว','DRINK',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR003','8850002003','โอเลี้ยง','Iced Black Coffee',35,10,'แก้ว','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR004','8850002004','มะนาวโซดา','Lemonade Soda',40,12,'แก้ว','DRINK',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR005','8850002005','น้ำเปล่า','Water',15,3,'ขวด','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR006','8850002006','โคโค่เย็น','Cocoa Iced',50,18,'แก้ว','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR007','8850002007','น้ำผลไม้ปั่น','Fresh Fruit Blend',60,20,'แก้ว','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR008','8850002008','นมสด','Fresh Milk',30,10,'แก้ว','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR009','8850002009','ชาเขียวเย็น','Iced Green Tea',45,14,'แก้ว','DRINK',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR010','8850002010','เปปซี่','Pepsi',30,10,'กระป๋อง','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR011','8850002011','สไปรท์','Sprite',30,10,'กระป๋อง','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR012','8850002012','เบียร์สิงห์','Singha Beer',80,45,'ขวด','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR013','8850002013','เบียร์ช้าง','Chang Beer',75,42,'ขวด','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR014','8850002014','น้ำมะพร้าว','Coconut Water',50,20,'ผล','DRINK',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDrink,'DR015','8850002015','สมูทตี้สตรอเบอรี่','Strawberry Smoothie',70,25,'แก้ว','DRINK',1,0,0,GETUTCDATE(),GETUTCDATE());

-- ========== หมวด ของหวาน ==========
INSERT INTO Products (Id,BranchId,CategoryId,Sku,Barcode,Name,NameEn,Price,CostPrice,Unit,ProductType,IsActive,IsFeatured,Taxable,CreatedAt,UpdatedAt)
VALUES
(NEWID(),@BranchId,@CatDessert,'DS001','8850003001','ข้าวเหนียวมะม่วง','Mango Sticky Rice',80,30,'จาน','DESSERT',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS002','8850003002','บัวลอยน้ำขิง','Taro Balls Ginger',50,18,'ชาม','DESSERT',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS003','8850003003','วุ้นกะทิ','Coconut Jelly',40,12,'ชิ้น','DESSERT',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS004','8850003004','เฉาก๊วยนม','Grass Jelly Milk',45,15,'แก้ว','DESSERT',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS005','8850003005','ไอศกรีมกะทิ','Coconut Ice Cream',60,22,'ถ้วย','DESSERT',1,1,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS006','8850003006','ทองหยิบ','Egg Yolk Drops',30,10,'ชิ้น','DESSERT',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS007','8850003007','ฝอยทอง','Golden Egg Threads',30,10,'ชิ้น','DESSERT',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS008','8850003008','ขนมครก','Coconut Pancake',35,12,'จาน','DESSERT',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS009','8850003009','กล้วยบวชชี','Banana in Coconut Milk',45,15,'ชาม','DESSERT',1,0,0,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatDessert,'DS010','8850003010','เค้กช็อคโกแลต','Chocolate Cake',90,35,'ชิ้น','DESSERT',1,1,0,GETUTCDATE(),GETUTCDATE());

-- ========== หมวด สินค้าปลีก ==========
INSERT INTO Products (Id,BranchId,CategoryId,Sku,Barcode,Name,NameEn,Price,CostPrice,Unit,ProductType,IsActive,IsFeatured,Taxable,CreatedAt,UpdatedAt)
VALUES
(NEWID(),@BranchId,@CatRetail,'RT001','8850004001','มาม่าหมู','Mama Pork Noodle',7,4,'ซอง','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT002','8850004002','มาม่าไก่','Mama Chicken Noodle',7,4,'ซอง','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT003','8850004003','มาม่าต้มยำกุ้ง','Mama Tom Yum Shrimp',7,4,'ซอง','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT004','8850004004','ข้าวสารหอมมะลิ 5 กก.','Jasmine Rice 5kg',250,180,'ถุง','RETAIL',1,1,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT005','8850004005','น้ำมันพืชไทย 1 ลิตร','Vegetable Oil 1L',65,45,'ขวด','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT006','8850004006','ซีอิ๊วขาวแม่ประนอม 700ml','Soy Sauce 700ml',52,35,'ขวด','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT007','8850004007','น้ำปลาทิพรส 700ml','Fish Sauce 700ml',48,32,'ขวด','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT008','8850004008','ไข่ไก่ (แผง 30 ฟอง)','Eggs 30pcs',130,95,'แผง','RETAIL',1,1,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT009','8850004009','นมโฟร์โมสต์ 1 ลิตร','Foremost Milk 1L',52,38,'กล่อง','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT010','8850004010','ผงชูรสอายิโนะโมะโต๊ะ 500g','MSG 500g',38,25,'ถุง','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT011','8850004011','น้ำตาลทราย 1 กก.','Sugar 1kg',25,18,'ถุง','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT012','8850004012','เกลือป่น 500g','Salt 500g',12,7,'ถุง','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT013','8850004013','สบู่ลักส์ก้อน','Lux Soap Bar',28,18,'ก้อน','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT014','8850004014','แชมพูซันซิลก์ 180ml','Sunsilk Shampoo 180ml',55,35,'ขวด','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT015','8850004015','ยาสีฟันโคลเกต 160g','Colgate Toothpaste 160g',48,32,'หลอด','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT016','8850004016','ผ้าอนามัยวิสเปอร์','Whisper Sanitary Pad',89,60,'แพ็ก','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT017','8850004017','กระดาษทิชชู่เพรสทีจ 3 ม้วน','Prestige Tissue 3 Rolls',55,38,'แพ็ก','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT018','8850004018','ถุงขยะดำ 18x20','Black Garbage Bag 18x20',25,15,'แพ็ก','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT019','8850004019','น้ำยาล้างจาน Sunlight 500ml','Sunlight Dish Soap 500ml',38,25,'ขวด','RETAIL',1,0,1,GETUTCDATE(),GETUTCDATE()),
(NEWID(),@BranchId,@CatRetail,'RT020','8850004020','ผงซักฟอก Breeze 900g','Breeze Detergent 900g',75,52,'ถุง','RETAIL',1,1,1,GETUTCDATE(),GETUTCDATE());

-- ========== เพิ่ม Stock ให้สินค้าใหม่ ==========
INSERT INTO ProductStocks (Id, BranchId, ProductId, PhysicalQty, ReservedQty, MinAlertQty, UpdatedAt)
SELECT NEWID(), @BranchId, p.Id,
    CASE p.ProductType 
        WHEN 'FOOD'    THEN 50
        WHEN 'DRINK'   THEN 80
        WHEN 'DESSERT' THEN 30
        ELSE 200
    END,
    0,
    CASE p.ProductType 
        WHEN 'FOOD'    THEN 10
        WHEN 'DRINK'   THEN 20
        WHEN 'DESSERT' THEN 5
        ELSE 30
    END,
    GETUTCDATE()
FROM Products p
WHERE p.BranchId = @BranchId;
GO

-- ========== ลูกค้า ==========
DECLARE @BranchId UNIQUEIDENTIFIER = '44538349-F06D-4457-8049-5D47A1DC8A69';
INSERT INTO Customers (Id, BranchId, Name, Phone, Email, Points, TotalSpent, MemberLevel, IsActive, CreatedAt)
VALUES
(NEWID(),@BranchId,'สมชาย ใจดี','0812345678','somchai@email.com',1250,12500,'GOLD',1,DATEADD(day,-90,GETUTCDATE())),
(NEWID(),@BranchId,'วิไล สุขใจ','0823456789','wilai@email.com',850,8500,'SILVER',1,DATEADD(day,-60,GETUTCDATE())),
(NEWID(),@BranchId,'ประเสริฐ มั่งมี','0834567890',NULL,320,3200,'BRONZE',1,DATEADD(day,-30,GETUTCDATE())),
(NEWID(),@BranchId,'นิภา รักดี','0845678901','nipa@email.com',2100,21000,'PLATINUM',1,DATEADD(day,-120,GETUTCDATE())),
(NEWID(),@BranchId,'อนันต์ สมบูรณ์','0856789012',NULL,0,0,NULL,1,DATEADD(day,-5,GETUTCDATE())),
(NEWID(),@BranchId,'พรทิพย์ แก้วใส','0867890123','porntip@email.com',430,4300,'BRONZE',1,DATEADD(day,-45,GETUTCDATE())),
(NEWID(),@BranchId,'สุชาติ ทองดี','0878901234',NULL,1580,15800,'GOLD',1,DATEADD(day,-75,GETUTCDATE())),
(NEWID(),@BranchId,'มาลี วงศ์งาม','0889012345','malee@email.com',720,7200,'SILVER',1,DATEADD(day,-50,GETUTCDATE())),
(NEWID(),@BranchId,'วิชัย ศรีสวัสดิ์','0890123456',NULL,100,1000,NULL,1,DATEADD(day,-10,GETUTCDATE())),
(NEWID(),@BranchId,'กนกพร ดาวเรือง','0801234567','kanokporn@email.com',3200,32000,'PLATINUM',1,DATEADD(day,-180,GETUTCDATE())),
(NEWID(),@BranchId,'ธนภัทร จันทร์เพ็ญ','0812222333',NULL,560,5600,'SILVER',1,DATEADD(day,-35,GETUTCDATE())),
(NEWID(),@BranchId,'อรอุมา ไพรสน','0823333444','ornuma@email.com',890,8900,'SILVER',1,DATEADD(day,-55,GETUTCDATE())),
(NEWID(),@BranchId,'ณัฐพล รุ่งเรือง','0834444555',NULL,210,2100,'BRONZE',1,DATEADD(day,-20,GETUTCDATE())),
(NEWID(),@BranchId,'ปิยนันท์ สายใจ','0845555666','piyanun@email.com',4500,45000,'PLATINUM',1,DATEADD(day,-200,GETUTCDATE())),
(NEWID(),@BranchId,'สิริมา บุญมา','0856666777',NULL,0,450,NULL,1,DATEADD(day,-3,GETUTCDATE()));
GO

PRINT 'Real data seeded successfully: Products + Customers';
