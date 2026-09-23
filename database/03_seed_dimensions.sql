-- Québec EmploiVision — 03 : dimensions de référence.
-- Régions administratives du Québec (noms officiels),
-- professions (CNP 2021) et industries (SCIAN) réellement utilisées au Québec.

USE EmploiVision;
GO

IF NOT EXISTS (SELECT 1 FROM DimRegion)
INSERT INTO DimRegion (RegionCode, RegionName) VALUES
    ('01', 'Bas-Saint-Laurent'),
    ('02', 'Saguenay–Lac-Saint-Jean'),
    ('03', 'Capitale-Nationale'),
    ('04', 'Mauricie'),
    ('05', 'Estrie'),
    ('06', 'Montréal'),
    ('07', 'Outaouais'),
    ('08', 'Abitibi-Témiscamingue'),
    ('09', 'Côte-Nord'),
    ('10', 'Nord-du-Québec'),
    ('11', 'Gaspésie–Îles-de-la-Madeleine'),
    ('12', 'Chaudière-Appalaches'),
    ('13', 'Laval'),
    ('14', 'Lanaudière'),
    ('15', 'Laurentides'),
    ('16', 'Montérégie'),
    ('17', 'Centre-du-Québec');
GO

IF NOT EXISTS (SELECT 1 FROM DimProfession)
INSERT INTO DimProfession (ProfessionCode, ProfessionName) VALUES
    ('10010', 'Directeurs et directrices, gestionnaires (CNP 00)'),
    ('20010', 'Professionnels et professionnelles des sciences naturelles et appliquées'),
    ('21211', 'Analystes de données et analystes en informatique'),
    ('21220', 'Spécialistes en cyber-sécurité'),
    ('21221', 'Concepteurs et conceptrices de systèmes informatiques'),
    ('21222', 'Développeurs informatiques et développeuses en informatique'),
    ('21223', 'Concepteurs et conceptrices de sites Web'),
    ('21230', 'Concepteurs et conceptrices logiciels et programmeuses et programmeurs'),
    ('22221', 'Techniciens en conception et techniciennes en design numérique'),
    ('32101', 'Infirmiers et infirmières auxiliaires'),
    ('31100', 'Médecins spécialistes et médecins de famille'),
    ('41402', 'Analystes de recherche et analystes en statistiques'),
    ('60010', 'Représentants et représentantes des ventes'),
    ('64300', 'Vendeurs et vendeuses au détail'),
    ('75110', 'Travailleurs de la construction'),
    ('22210', 'Techniciens en informatique et techniciennes en informatique');
GO

IF NOT EXISTS (SELECT 1 FROM DimIndustry)
INSERT INTO DimIndustry (IndustryCode, IndustryName) VALUES
    ('11', 'Agriculture, foresterie, pêche et chasse'),
    ('21', 'Extraction minière, et exploitation en carrière'),
    ('22', 'Services publics'),
    ('23', 'Construction'),
    ('31-33', 'Fabrication'),
    ('41', 'Commerce de gros'),
    ('44-45', 'Commerce de détail'),
    ('48-49', 'Transport et entreposage'),
    ('51', 'Industrie de l''information et industrie culturelle'),
    ('52', 'Finances et assurances'),
    ('53', 'Services immobiliers'),
    ('54', 'Services professionnels, scientifiques et techniques'),
    ('56', 'Services administratifs et services de soutien'),
    ('62', 'Soins de santé et assistance sociale'),
    ('72', 'Services d''hébergement et de restauration'),
    ('81', 'Autres services'),
    ('91', 'Administrations publiques');
GO
