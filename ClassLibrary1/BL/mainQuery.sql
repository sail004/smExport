select
 c.articul as id,
  c.name as name,
  '' as info,
  o.name_country as name_country,
  mf.name_manufacturer,
  List(b.barcode, ', ') as barcodes,
  cli.name_clients as name_store,
  max(r.quantity) as rest,
  max(d.price_rub ) as price,
  c.classif,
  cl.name_classif as name_group,
  max(s.cash_moddate) as modification_time

 from cardscla c
 left join bar b on b.articul = c.articul
 left join country o on o.id_country = c.country
 left join manufacturer mf on mf.id_manufacturer=c.manufacturer
 left join cardparam_strong s on s.articul = c.articul
 left join disccard d on d.articul = c.articul
 left join pricekind k on k.id_pricekind=d.price_kind and k.kind=0
 left join th_classbyclient t on t.th_classif=k.filialindex_pricekind
 left join ostatok_short r on r.articul=c.articul and r.place_index=t.client_index
 left join clients cli on cli.id_clients=r.place_index
 left join classif cl on cl.id_classif = c.classif

 where  c.articul != '<CLASSIF>' and c.mesuriment > 0 and c.classif > 0  and d.price_rub>0 and d.price_rub is not null

 group by   c.articul ,
  c.name ,
  o.name_country ,
  mf.name_manufacturer,
  cli.name_clients,
  c.classif,
  cl.name_classif

having max(s.cash_moddate)>'@date'