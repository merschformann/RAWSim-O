reset
# Output definition
set terminal pdfcairo enhanced size 7, 3 font "Consolas, 12"
set lmargin 13
set rmargin 13
# Parameters
set key right top Right
set grid
set style fill solid 0.75
# Line-Styles
set style line 1 linetype 1 linecolor rgb "#7090c8" linewidth 1
set output "myInventoryProfile.pdf"
set title "myInventoryProfile"
set xlabel "Frequency"
set ylabel "SKU count"
plot \
"myInventoryProfilegroups.dat" u 1:2 w boxes linestyle 1 t "SKU frequencies"
set title "myInventoryProfile"
set xlabel "SKU"
set ylabel "frequency"
plot \
"myInventoryProfilesimple.dat" u 1:2 w steps linestyle 1 t "SKU frequencies"
set title "myInventoryProfile"
set xlabel "SKU"
set ylabel "rel. cum. frequency"
plot \
"myInventoryProfilecumulative.dat" u 1:2 w steps linestyle 1 t "cum. SKU frequencies"
set title "myInventoryProfile"
set xlabel "SKU"
set ylabel "probability"
plot \
"myInventoryProfileprobability.dat" u 1:2 w steps linestyle 1 t "SKU probabilities"
set title "myInventoryProfile"
set xlabel "SKU"
set ylabel "size"
plot \
"myInventoryProfileweights.dat" u 1:2 w steps linestyle 1 t "SKU size"
set title "myInventoryProfile"
set xlabel "SKU"
set ylabel "units"
plot \
"myInventoryProfilebundlesizes.dat" u 1:2 w steps linestyle 1 t "SKU replenishment order size"
reset
exit
