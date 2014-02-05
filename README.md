![Queen Command-line Compiler Interface](http://yvt.jp/queen/img/queen-clic-2.png)
=======

.NET出力を主なターゲットとしたQ言語コンパイラです。

[ツイート](https://twitter.com/intent/tweet?original_referer=https%3A%2F%2Fabout.twitter.com%2Fresources%2Fbuttons&text=Twitter%20Buttons%20%7C%20About&tw_p=tweetbutton&url=https%3A%2F%2Fgithub.com%2Fyvt%2Fqueen-compiler&via=YVT)

サンプルコード
------

	{ エラトステネスのふるい実装 }
	class PrimeGenesis
	    var primes: []int:: #[10000]int
	    var numPrimes: int
	    ctor()
	        primes[0] :: 2
	        numPrimes :: 1
	    end ctor
	    -func IsComposite(x: int): bool
	        foreach prime (primes)
	            if( prime = 0 )
	                return false
	            elif( x % prime = 0 )
	                return true
	            end if
	        end foreach
	        return false
	    end func
	    func GetNext(): int
	        var i :: primes[numPrimes - 1] + 1
	        while (IsComposite(i))
	            i :+ 1
	        end while
	        primes[numPrimes] :: i
	        numPrimes :+ 1
	        return i
	    end func
	end class
	
	{ エントリポイント }
	func AppMain()
	    var primes :: #PrimeGenesis
	    var sum :: 2
	    var startTime :: Time@Sys()
	    for (2, 10000)
	        sum :+ primes.GetNext()
	    end for
	
	    var duration :: (Time@Sys() - startTime)$float / 1000.0
	
	    Dbg@Log("10000番目までの素数の和 = " ~ sum.ToStr() ~ " (" ~ duration.ToStr() ~ " 秒)")
	end func

